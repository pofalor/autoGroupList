using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GroupListNet.Core.src.Services.Impl
{
    public class LeaderService : ILeaderService
    {
        private readonly ILogger<LeaderService> _logger;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly string[] _mainLeaderTelegramIds;

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

            _mainLeaderTelegramIds = telegramConfig?.LeaderTelegramIdsArray ?? [];
        }

        public bool IsMainLeader(string? telegramId)
        {
            return !string.IsNullOrWhiteSpace(telegramId) && _mainLeaderTelegramIds.Contains(telegramId);
        }

        public async Task<bool> IsLeaderAsync(string? telegramId)
        {
            if (string.IsNullOrWhiteSpace(telegramId))
                return false;

            // Основной староста может пользоваться командами, даже если он ещё не зарегистрирован как студент
            return IsMainLeader(telegramId) || await _leaderRepository.IsLeaderAsync(telegramId);
        }

        public async Task SyncMainLeadersAsync()
        {
            if (_mainLeaderTelegramIds.Length == 0)
            {
                _logger.LogWarning("В конфиге не задан {SectionName}:{SettingName}, основного старосты нет. " +
                    "Права старосты в этом случае можно выдать только записью в таблице leader.",
                    TelegramSettingsConfiguration.TelegramSectionInConfig, nameof(TelegramSettingsConfiguration.LeaderTelegramIds));
                return;
            }

            var addedAnyLeader = false;

            foreach (var mainLeaderTelegramId in _mainLeaderTelegramIds)
            {
                var student = await _studentRepository.GetByTelegramIdAsync(mainLeaderTelegramId);
                if (student == null)
                    continue;

                if (await _leaderRepository.IsLeaderAsync(mainLeaderTelegramId))
                    continue;

                await _leaderRepository.AddAsync(new Leader { StudentId = student.Id });
                addedAnyLeader = true;

                _logger.LogInformation("Студент {StudentId} назначен старостой по телеграм айди {TelegramId} из конфига.",
                    student.Id, mainLeaderTelegramId);
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
            var registeredStudents = await _studentRepository.GetStudentsWithTelegramAsync();

            return OrderByNumber(registeredStudents.Where(student => !leaderStudentIds.Contains(student.Id)));
        }

        public async Task<IReadOnlyCollection<Student>> GetRemovableAssistantsAsync()
        {
            var leaderStudents = await _leaderRepository.GetLeaderStudentsAsync();

            return OrderByNumber(leaderStudents.Where(student => !IsMainLeader(student.TelegramId)));
        }

        public async Task<IDataResult<Student>> AddAssistantAsync(int studentId)
        {
            var result = new DataResult<Student>();

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || student.IsDeleted)
                return result.WithError("Студент не найден.");

            if (string.IsNullOrWhiteSpace(student.TelegramId))
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

            if (IsMainLeader(student.TelegramId))
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
