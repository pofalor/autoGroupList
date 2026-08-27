using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GroupListNet.Core.src.Services.Impl
{
    public class LeaderService : ILeaderService
    {
        private readonly ILogger<LeaderService> _logger;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IStudentRepository _studentRepository;

        /// <summary>
        /// Айди основных старост из конфига по каждому мессенджеру
        /// </summary>
        private readonly IReadOnlyDictionary<MessengerType, string[]> _mainLeaderIds;

        /// <summary>
        /// Айди администраторов из конфига по каждому мессенджеру
        /// </summary>
        private readonly IReadOnlyDictionary<MessengerType, string[]> _adminIds;

        public LeaderService(ILogger<LeaderService> logger,
            IConfiguration config,
            ILeaderRepository leaderRepository,
            IStudentRepository studentRepository)
        {
            _logger = logger;
            _leaderRepository = leaderRepository;
            _studentRepository = studentRepository;

            var telegramConfig = config.GetSection(TelegramSettingsConfiguration.TelegramSectionInConfig)
                .Get<TelegramSettingsConfiguration>();
            var vkConfig = config.GetSection(VkSettingsConfiguration.VkSectionInConfig)
                .Get<VkSettingsConfiguration>();

            _mainLeaderIds = new Dictionary<MessengerType, string[]>
            {
                [MessengerType.Telegram] = telegramConfig?.LeaderTelegramIdsArray ?? [],
                [MessengerType.Vk] = vkConfig?.LeaderVkIdsArray ?? []
            };

            _adminIds = new Dictionary<MessengerType, string[]>
            {
                [MessengerType.Telegram] = telegramConfig?.AdminTelegramIdsArray ?? [],
                [MessengerType.Vk] = vkConfig?.AdminVkIdsArray ?? []
            };
        }

        public bool IsMainLeader(MessengerType messenger, string? messengerId)
        {
            return !string.IsNullOrWhiteSpace(messengerId)
                && _mainLeaderIds.TryGetValue(messenger, out var ids)
                && ids.Contains(messengerId);
        }

        public bool IsMainLeader(Student student)
        {
            return student.GetLinkedMessengers()
                .Any(messenger => IsMainLeader(messenger, student.GetMessengerId(messenger)));
        }

        public bool IsAdmin(MessengerType messenger, string? messengerId)
        {
            return !string.IsNullOrWhiteSpace(messengerId)
                && _adminIds.TryGetValue(messenger, out var ids)
                && ids.Contains(messengerId);
        }

        public async Task<bool> IsLeaderAsync(MessengerType messenger, string? messengerId)
        {
            if (string.IsNullOrWhiteSpace(messengerId))
                return false;

            // Основной староста может пользоваться командами, даже если он ещё не зарегистрирован как студент
            return IsMainLeader(messenger, messengerId) || await _leaderRepository.IsLeaderAsync(messenger, messengerId);
        }

        public async Task SyncMainLeadersAsync()
        {
            if (_mainLeaderIds.Values.All(ids => ids.Length == 0))
            {
                _logger.LogWarning("В конфиге не заданы {TelegramSection}:{TelegramSetting} и {VkSection}:{VkSetting}, " +
                    "основного старосты нет. Права старосты в этом случае можно выдать только записью в таблице leader.",
                    TelegramSettingsConfiguration.TelegramSectionInConfig, nameof(TelegramSettingsConfiguration.LeaderTelegramIds),
                    VkSettingsConfiguration.VkSectionInConfig, nameof(VkSettingsConfiguration.LeaderVkIds));
                return;
            }

            var addedAnyLeader = false;
            // Один и тот же студент может быть указан основным старостой сразу в двух мессенджерах
            var alreadyAddedStudentIds = new HashSet<int>();

            foreach (var (messenger, mainLeaderIds) in _mainLeaderIds)
            {
                foreach (var mainLeaderId in mainLeaderIds)
                {
                    var student = await _studentRepository.GetByMessengerIdAsync(messenger, mainLeaderId);
                    if (student == null)
                        continue;

                    if (alreadyAddedStudentIds.Contains(student.Id))
                        continue;

                    if (await _leaderRepository.IsLeaderAsync(messenger, mainLeaderId))
                        continue;

                    await _leaderRepository.AddAsync(new Leader { StudentId = student.Id });
                    alreadyAddedStudentIds.Add(student.Id);
                    addedAnyLeader = true;

                    _logger.LogInformation("Студент {StudentId} назначен старостой по айди {MessengerId} в {Messenger} из конфига.",
                        student.Id, mainLeaderId, messenger);
                }
            }

            if (addedAnyLeader)
                await _leaderRepository.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<Student>> GetLeadersAsync()
        {
            return OrderByNumber(await _leaderRepository.GetLeaderStudentsAsync());
        }

        public async Task<IReadOnlyCollection<Student>> GetAssistantCandidatesAsync()
        {
            var leaderStudentIds = (await _leaderRepository.GetLeaderIds()).ToHashSet();
            var registeredStudents = await _studentRepository.GetStudentsWithAnyMessengerAsync();

            return OrderByNumber(registeredStudents.Where(student => !leaderStudentIds.Contains(student.Id)));
        }

        public async Task<IReadOnlyCollection<Student>> GetRemovableAssistantsAsync()
        {
            var leaderStudents = await _leaderRepository.GetLeaderStudentsAsync();

            return OrderByNumber(leaderStudents.Where(student => !IsMainLeader(student)));
        }

        public async Task<IDataResult<Student>> AddAssistantAsync(int studentId)
        {
            var result = new DataResult<Student>();

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || student.IsDeleted)
                return result.WithError("Студент не найден.");

            if (!student.HasAnyMessenger())
                return result.WithError($"{student.GetFullName()} ещё не зарегистрирован в боте, назначить помощником нельзя.");

            if (await IsLeaderStudentAsync(student.Id))
                return result.WithError($"{student.GetFullName()} уже имеет права старосты.");

            await _leaderRepository.AddAsync(new Leader { StudentId = student.Id });
            await _leaderRepository.SaveChangesAsync();

            _logger.LogInformation("Студент {StudentId} назначен помощником старосты.", student.Id);

            return result.WithData(student);
        }

        public async Task<IDataResult<Student>> RemoveAssistantAsync(int studentId)
        {
            var result = new DataResult<Student>();

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || student.IsDeleted)
                return result.WithError("Студент не найден.");

            if (IsMainLeader(student))
                return result.WithError($"{student.GetFullName()} — основной староста, снять его можно только через конфиг.");

            if (!await IsLeaderStudentAsync(student.Id))
                return result.WithError($"{student.GetFullName()} не является помощником старосты.");

            await _leaderRepository.DeleteLeaderByStudentIdsAsync([student.Id]);

            _logger.LogInformation("Со студента {StudentId} сняты права помощника старосты.", student.Id);

            return result.WithData(student);
        }

        private async Task<bool> IsLeaderStudentAsync(int studentId)
        {
            return (await _leaderRepository.GetLeaderIds()).Contains(studentId);
        }

        private static IReadOnlyCollection<Student> OrderByNumber(IEnumerable<Student> students)
        {
            return students.OrderBy(student => student.NumberInGroup).ToArray();
        }
    }
}
