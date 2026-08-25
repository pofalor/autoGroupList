using AutoMapper;
using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.DataAccess;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Entities.Utils;
using GroupListNet.Core.src.Enums;
using GroupListNet.Core.src.ParseGroupList;
using GroupListNet.Core.src.ParseSchedule;
using GroupListNet.Core.src.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace GroupListNet.Core.src.Bot
{
    /// <summary>
    /// Логика бота, общая для всех мессенджеров. Транспорт приходит параметром <see cref="IMessengerClient"/>,
    /// поэтому ответ уходит туда же, откуда пришло сообщение
    /// </summary>
    public class BotUpdateHandler
    {
        private readonly ILogger<BotUpdateHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEnumerable<IMessengerClient> _clients;
        private readonly TimeSpan _timeLimitHourUtc;

        /// <summary>
        /// Старосты, от которых бот ждёт расписание. Ключ с мессенджером, иначе числовые айди разных
        /// мессенджеров могут совпасть
        /// </summary>
        private readonly HashSet<(MessengerType Messenger, string UserId)> WaitingLeaders = [];

        /// <summary>
        /// Старосты, от которых бот ждёт список группы
        /// </summary>
        private readonly HashSet<(MessengerType Messenger, string UserId)> WaitingGroupListLeaders = [];

        /// <summary>
        /// Префикс кнопок выбора того, делится ли группа на подгруппы
        /// </summary>
        private const string SubgroupsSettingCallbackPrefix = "setdivision_";

        /// <summary>
        /// Префикс кнопок назначения помощника старосты
        /// </summary>
        private const string AddLeaderCallbackPrefix = "addleader_";

        /// <summary>
        /// Префикс кнопок снятия помощника старосты
        /// </summary>
        private const string RemoveLeaderCallbackPrefix = "removeleader_";

        private const string GroupListFormatExample = """
Формат списка группы:
1. Иванов Иван Иванович
2. Иванов Илья Юрьевич
...

Нумерацию можно не ставить — тогда номера проставятся по порядку строк.
""";

        public BotUpdateHandler(ILogger<BotUpdateHandler> logger,
            IConfiguration config,
            IServiceScopeFactory scopeFactory,
            IEnumerable<IMessengerClient> clients)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _clients = clients;

            var timeLimitHourUtc = 17;
            try
            {
                var timeLimitHourStr = config.GetSection(SendingSettings.SendingSettingsSectionInConfig).Get<SendingSettings>()?.DeadlineTime;
                if (!TimeSpan.TryParse(timeLimitHourStr, CultureInfo.InvariantCulture, out _timeLimitHourUtc))
                {
                    _timeLimitHourUtc = new TimeSpan(timeLimitHourUtc, 0, 0);
                    using var scope = _scopeFactory.CreateScope();
                    var logNotificatiorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
                    logNotificatiorService.LogAndNotifyAdminsAsync($"Ошибка в {nameof(BotUpdateHandler)}, " +
                        $"не удалось спарсить DeadlineTime из конфига. Используемое значение = {timeLimitHourUtc}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} error getting value from config! Using default value = {DefaultValue}{NewLine}",
                    nameof(BotUpdateHandler), timeLimitHourUtc, Environment.NewLine);
            }
        }

        public async Task HandleMessageAsync(IMessengerClient client, BotMessage message)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                    return;

                var userId = message.UserId;
                var text = message.Text;
                using var scope = _scopeFactory.CreateScope();
                var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
                var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                var leaderService = scope.ServiceProvider.GetRequiredService<ILeaderService>();
                var groupSettingsRepo = scope.ServiceProvider.GetRequiredService<IGroupSettingsRepository>();

                switch (text.ToLower())
                {
                    case "/start":
                        await StartCommandAsync(client, message, userId, studentRepo);
                        break;
                    case "/help":
                        await HelpCommandAsync(client, message, userId, leaderService, groupSettingsRepo);
                        break;
                    case "/info":
                        await InfoCommandAsync(client, message, userId, studentRepo, groupSettingsRepo);
                        break;
                    case "/schedule":
                        await ScheduleCommandAsync(client, message, userId, studentRepo, scheduleRepo);
                        break;
                    case "/attend_other_group":
                        if (await EnsureSubgroupsEnabledAsync(client, message, userId, groupSettingsRepo))
                            await AttendOtherGroupCommandAsync(client, message, userId, studentRepo, scheduleRepo, attendanceRepo);
                        break;
                    case "/set_subgroup":
                        if (await EnsureSubgroupsEnabledAsync(client, message, userId, groupSettingsRepo))
                            await SetSubgroupCommandAsync(client, message, userId, studentRepo);
                        break;
                    case "/remove_subgroup":
                        if (await EnsureSubgroupsEnabledAsync(client, message, userId, groupSettingsRepo))
                            await RemoveSubgroupCommandAsync(client, message, userId, studentRepo);
                        break;
                    case "/list": // команда для старосты
                        await ListCommandAsync(client, message, userId, studentRepo, attendanceRepo, leaderService);
                        break;
                    case "/time": // команда для администратора
                        await TimeCommandAsync(client, message, userId, studentRepo, leaderService);
                        break;
                    case "/parse_schedule": // команда для старосты
                        await ParseScheduleCommandAsync(client, message, userId, leaderService, studentRepo);
                        break;
                    case "/parse_group_list": // команда для старосты
                        await ParseGroupListCommandAsync(client, message, userId, leaderService, studentRepo);
                        break;
                    case "/subgroups": // команда для старосты
                        await SubgroupsSettingCommandAsync(client, message, userId, leaderService, groupSettingsRepo, studentRepo);
                        break;
                    case "/add_leader": // команда для старосты
                        await AddLeaderCommandAsync(client, message, userId, leaderService, studentRepo);
                        break;
                    case "/remove_leader": // команда для старосты
                        await RemoveLeaderCommandAsync(client, message, userId, leaderService, studentRepo);
                        break;
                    default:
                        var waitingKey = (client.Messenger, userId);
                        if (WaitingLeaders.Contains(waitingKey))
                        {
                            await ProcessScheduleTextAsync(client, message, userId);
                            WaitingLeaders.Remove(waitingKey);
                        }
                        else if (WaitingGroupListLeaders.Contains(waitingKey))
                        {
                            await ProcessGroupListTextAsync(client, message, userId);
                            WaitingGroupListLeaders.Remove(waitingKey);
                        }
                        else if (int.TryParse(text, out int number))
                        {
                            await HandleNumberInputAsync(client, message, userId, number, studentRepo, leaderService);
                        }
                        else
                        {
                            await HandleOtherTextAsync(client, message, userId, studentRepo);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке текстового сообщения. Ошибка произошла в {ClassName}. " +
                    "Мессенджер={Messenger}. chatId={ChatId}. UserId={UserId}. Text={Text}",
                    nameof(BotUpdateHandler), message.Messenger, message.ChatId, message.UserId, message.Text);
                await SafeSendAsync(client, message.ChatId, "Произошла системная ошибка. Попробуйте ещё раз. " +
                    "Если ошибка повторится, напишите администратору", await CreateMainMenuAsync(client.Messenger));
            }
        }

        public async Task HandleCallbackAsync(IMessengerClient client, BotCallback callback)
        {
            try
            {
                var userId = callback.UserId;
                var callbackData = callback.Data;
                using var scope = _scopeFactory.CreateScope();
                var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
                var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                var leaderService = scope.ServiceProvider.GetRequiredService<ILeaderService>();
                var groupSettingsRepo = scope.ServiceProvider.GetRequiredService<IGroupSettingsRepository>();

                if (callbackData.StartsWith("setsubgroup_"))
                {
                    await HandleSubgroupChoiceAsync(client, callback, userId, callbackData, studentRepo);
                }
                else if (callbackData.StartsWith("attend_"))
                {
                    await HandleAttendanceAsync(client, callback, userId, callbackData, studentRepo, scheduleRepo, attendanceRepo);
                }
                else if (callbackData.StartsWith("markother_"))
                {
                    await HandleMarkOtherAttendanceAsync(client, callback, userId, callbackData, studentRepo, scheduleRepo, attendanceRepo);
                }
                else if (callbackData.StartsWith(SubgroupsSettingCallbackPrefix))
                {
                    await HandleSubgroupsSettingChoiceAsync(client, callback, userId, callbackData, leaderService, groupSettingsRepo, studentRepo);
                }
                else if (callbackData.StartsWith(AddLeaderCallbackPrefix))
                {
                    await HandleAddLeaderChoiceAsync(client, callback, userId, callbackData, leaderService);
                }
                else if (callbackData.StartsWith(RemoveLeaderCallbackPrefix))
                {
                    await HandleRemoveLeaderChoiceAsync(client, callback, userId, callbackData, leaderService);
                }
                else
                {
                    await client.AnswerCallbackAsync(callback, "Неизвестная команда.", showAlert: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке нажатия кнопки. Мессенджер={Messenger}. UserId={UserId}. Data={Data}",
                    callback.Messenger, callback.UserId, callback.Data);
                await SafeAnswerAsync(client, callback, "Произошла системная ошибка.", showAlert: true);
            }
        }

        private async Task StartCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo != null)
            {
                await client.SendMessageAsync(chatId, $"С возвращением, {studentInfo.Name}! 👋",
                    await CreateMainMenuAsync(messenger, userId));
            }
            else // Если не зарегистрирован в этом мессенджере
            {
                var students = await studentRepo.GetStudentsWithoutAccountAsync(messenger);
                if (!students.Any())
                {
                    await client.SendMessageAsync(chatId, "Не удалось загрузить список группы.");
                    return;
                }
                var groupList = string.Join(Environment.NewLine, students.OrderBy(s => s.NumberInGroup).Select(s => $"{ s.NumberInGroup}. {s.SurName} { s.Name} { s.FatherName}"));
                await client.SendMessageAsync(chatId, $"Привет! Список группы:{Environment.NewLine}{groupList}{Environment.NewLine}Введите свой номер: ", BotKeyboard.Remove);
            }
        }

        private async Task HelpCommandAsync(IMessengerClient client, BotMessage message, string? userId, ILeaderService leaderService, IGroupSettingsRepository groupSettingsRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            try
            {
                var replaceTextForLeader = string.Empty;
                var replaceTextForAdmin = string.Empty;

                if (await leaderService.IsLeaderAsync(messenger, userId))
                {
                    replaceTextForLeader = @"
/list - Получить  отметившихся студентов.
/parse_schedule - Отправить боту новое расписание. Отправлять нужно от телеграмм бота @knrtukaibot.
/parse_group_list - Отправить боту новый список группы.
/subgroups - Задать, делится ли группа на подгруппы.
/add_leader - Назначить помощника старосты.
/remove_leader - Снять помощника старосты.
";
                }

                if (leaderService.IsAdmin(messenger, userId))
                    replaceTextForAdmin = $"/time - Получить текущее время (аналог ping).";

                var gmtPlus3Time = _timeLimitHourUtc.Add(TimeSpan.FromHours(3));

                // Команды подгрупп показываем, только если староста включил деление группы на подгруппы
                var replaceTextForSubgroups = await groupSettingsRepo.HasSubgroupsAsync()
                    ? @"
/set_subgroup - Присоединиться к подгруппе (1 или 2).
/remove_subgroup - Покинуть текущую подгруппу.
/attend_other_group - Отметиться на паре другой подгруппы (только если вы состоите в подгруппе)."
                    : string.Empty;

                var helpText = $@"
**Справка по боту** 🤖
Я помогу тебе отслеживать расписание и отмечаться на парах.
**Доступные команды:**
/start - Начало работы / приветствие.
/schedule - Расписание на сегодня.
/info - Ваша информация (ФИО, номер, подгруппа).{replaceTextForSubgroups}
/help - Эта справка.{replaceTextForLeader}{replaceTextForAdmin}
Уведомления о начале пар приходят автоматически.
Отмечаться можно до {gmtPlus3Time.Hours:00}:{gmtPlus3Time.Minutes:00} по МСК.
";

                await client.SendMessageAsync(chatId, helpText, await CreateMainMenuAsync(messenger, userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании сообщения помощи.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при формировании сообщения. Если ошибка повторится, обратитесь к администратору.",
                    await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task InfoCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo, IGroupSettingsRepository groupSettingsRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo == null)
            {
                await client.SendMessageAsync(chatId, "Вы не зарегистрированы. Используйте /start.", BotKeyboard.Remove);
                return;
            }
            var infoText = $@"
**Ваша информация:**
👤 **ФИО:** {studentInfo.SurName} {studentInfo.Name} {studentInfo.FatherName}
🔢 **Номер в группе:** {studentInfo.NumberInGroup}
";
            // Если группа не делится на подгруппы, показывать подгруппу студенту незачем
            if (await groupSettingsRepo.HasSubgroupsAsync())
            {
                if (studentInfo.Subgroup.HasValue)
                {
                    infoText += $"{Environment.NewLine}🎓 **Подгруппа:** {(studentInfo.Subgroup.Value == Subgroup.First ? "1-я" : "2-я")}{Environment.NewLine}";
                }
                else
                {
                    infoText += $"{Environment.NewLine}🎓 **Подгруппа:** Не указана (посещаете пары обеих подгрупп){Environment.NewLine}";
                }
            }
            await client.SendMessageAsync(chatId, infoText, await CreateMainMenuAsync(messenger, userId), markdown: true);
        }

        private async Task ScheduleCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo, IScheduleRepository scheduleRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo == null)
            {
                await client.SendMessageAsync(chatId, "Вы не зарегистрированы. Используйте /start.", BotKeyboard.Remove);
                return;
            }
            // Используем UTC время для получения текущего дня и недели
            var utcNow = DateTime.UtcNow;
            var localToday = DateOnly.FromDateTime(utcNow);
            var weekNumber = ISOWeek.GetWeekOfYear(utcNow);
            var weekType = (weekNumber % 2 == 0) ? WeekType.Even : WeekType.Odd;
            var dayName = utcNow.DayOfWeek;
            try
            {
                // Используем GetTodayScheduleAsync из репозитория, передавая текущую подгруппу (может быть null)
                var scheduleData = await scheduleRepo.GetTodayScheduleAsync(studentInfo.Subgroup, weekType, dayName);
                var scheduleText = FormatSchedule(scheduleData);
                var dateStr = localToday.ToString(DateFormatConstants.DatewithoutTimeZone);
                var header = $"Ваше расписание на сегодня ({dateStr}, {dayName}, {(weekType == WeekType.Even ? "четная" : "нечетная")} неделя):{Environment.NewLine}";
                await client.SendMessageAsync(chatId, header + scheduleText, await CreateMainMenuAsync(messenger, userId), markdown: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения расписания для студента {StudentId}", studentInfo.Id);
                await client.SendMessageAsync(chatId, "Не удалось получить расписание.", await CreateMainMenuAsync(messenger, userId), markdown: true);
            }
        }

        private async Task AttendOtherGroupCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo, IScheduleRepository scheduleRepo, IAttendanceRepository attendanceRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo == null)
            {
                await client.SendMessageAsync(chatId, "Вы не зарегистрированы. Используйте /start.", BotKeyboard.Remove);
                return;
            }

            // Проверка: если у студента нет подгруппы, он не может отмечаться на паре другой подгруппы
            if (!studentInfo.Subgroup.HasValue)
            {
                await client.SendMessageAsync(chatId, "Вы не состоите в подгруппе. Вы можете отмечаться на парах обеих подгрупп, используя кнопки из уведомлений о начале пары.",
                    await CreateMainMenuAsync(messenger, userId));
                return;
            }

            var currentSubgroup = studentInfo.Subgroup.Value; // Теперь безопасно, так как проверили
            var otherSubgroup = currentSubgroup == Subgroup.First ? Subgroup.Second : Subgroup.First;
            var otherSubgroupDisplay = currentSubgroup == Subgroup.First ? "2-й" : "1-й";

            if (!CanMarkAttendance())
            {
                await client.SendMessageAsync(chatId, $"К сожалению, время для отметки (до {_timeLimitHourUtc.Hours:00}:{_timeLimitHourUtc.Minutes:00} по UTC) уже вышло.", await CreateMainMenuAsync(messenger, userId));
                return;
            }
            // Используем UTC время
            var utcNow = DateTime.UtcNow;
            var localToday = DateOnly.FromDateTime(utcNow);
            var weekNumber = ISOWeek.GetWeekOfYear(utcNow);
            var weekType = (weekNumber % 2 == 0) ? WeekType.Even : WeekType.Odd;
            var dayName = utcNow.DayOfWeek;
            try
            {
                // Используем GetTodayScheduleAsync из репозитория для другой подгруппы
                var otherScheduleData = await scheduleRepo.GetTodayScheduleAsync(otherSubgroup, weekType, dayName);
                var dateStr = localToday.ToString("dd.MM.yyyy");
                if (!otherScheduleData.Any())
                {
                    await client.SendMessageAsync(chatId, $"У **{otherSubgroupDisplay} подгруппы** сегодня ({dateStr}, {dayName}, {(weekType == WeekType.Even ? "четная" : "нечетная")} неделя) пар нет.", await CreateMainMenuAsync(messenger, userId));
                    return;
                }
                var buttons = new List<BotButton>();
                var scheduleLines = new List<string> { $"Пары **{otherSubgroupDisplay} подгруппы** на сегодня ({dateStr}, {dayName}, {(weekType == WeekType.Even ? "четная" : "нечетная")} неделя):" };
                foreach (var scheduleItem in otherScheduleData)
                {
                    var callbackData = $"markother_{studentInfo.Id}_{scheduleItem.Id}";
                    buttons.Add(new BotButton($"{scheduleItem.StartTime} - {scheduleItem.Subject.Name}", callbackData));
                    scheduleLines.Add($"• {scheduleItem.StartTime} - {scheduleItem.Subject.Name}");
                }
                var scheduleText = string.Join("", scheduleLines);
                await client.SendMessageAsync(chatId, $"{scheduleText} Выберите пару: ", BotKeyboard.InlineColumn(buttons), markdown: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения расписания другой подгруппы для студента {StudentId}", studentInfo.Id);
                await client.SendMessageAsync(chatId, "Не удалось получить расписание другой подгруппы.");
            }
        }

        private async Task HandleNumberInputAsync(IMessengerClient client, BotMessage message, string userId, int number, IStudentRepository studentRepo, ILeaderService leaderService)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo != null) // Проверяем только регистрацию, не подгруппу
            {
                await client.SendMessageAsync(chatId, "Вы уже зарегистрированы.", await CreateMainMenuAsync(messenger, userId));
                return;
            }
            // Если студент не найден, ищем по номеру
            var studentExists = await studentRepo.GetByNumberInGroupAsync(number);
            if (studentExists == null)
            {
                await client.SendMessageAsync(chatId, "Такого номера нет.");
                return;
            }
            if (await studentRepo.IsNumberTakenAsync(number, messenger))
            {
                await client.SendMessageAsync(chatId, $"Номер {number} уже занят.");
                return;
            }
            try
            {
                // Сначала привязываем айди в мессенджере
                studentExists.SetMessengerId(messenger, userId);
                await studentRepo.UpdateAsync(studentExists);
                await studentRepo.SaveChangesAsync();

                // Основной староста задан в конфиге айди мессенджера, права выдаём сразу после его регистрации
                await leaderService.SyncMainLeadersAsync();

                _logger.LogInformation("Айди {UserId} в мессенджере {Messenger} привязан к номеру {Number}", userId, messenger, number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка привязки айди {UserId} в мессенджере {Messenger} к номеру {Number}", userId, messenger, number);
                await client.SendMessageAsync(chatId, "Ошибка сохранения номера.");
                return;
            }
            // Регистрация завершена, отправляем главное меню
            await client.SendMessageAsync(chatId, $"Номер {number} сохранен! Регистрация завершена. 👍", await CreateMainMenuAsync(messenger, userId));
        }

        private async Task HandleOtherTextAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo != null) // Проверяем только регистрацию
            {
                await client.SendMessageAsync(chatId, "Используйте кнопки меню или введите команду.", await CreateMainMenuAsync(messenger, userId));
            }
            else // Если не зарегистрирован
            {
                await client.SendMessageAsync(chatId, "Привет! Используйте /start для регистрации.", BotKeyboard.Remove);
            }
        }

        private async Task ListCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo, IAttendanceRepository attendanceRepo, ILeaderService leaderService)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            try
            {
                var isLeader = await leaderService.IsLeaderAsync(messenger, userId);

                // Проверка, является ли пользователь старостой
                if (!isLeader)
                {
                    await HandleOtherTextAsync(client, message, userId, studentRepo);
                    return;
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                // Получаем все отметки за сегодня
                var attendanceRecords = await attendanceRepo.GetTodayAttendanceAsync();

                var reportMessage = LeaderListFormatter.FormatGroupList(attendanceRecords, today);

                await client.SendMessageAsync(chatId, reportMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании списка отметившихся.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при формировании списка.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task TimeCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo, ILeaderService leaderService)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;

            try
            {
                // Проверка, является ли пользователь администратором
                if (!leaderService.IsAdmin(messenger, userId))
                {
                    await HandleOtherTextAsync(client, message, userId, studentRepo);
                    return;
                }

                var utcNow = DateTime.UtcNow.TimeOfDay;
                var gmtNow = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(3)).TimeOfDay;

                var responseMessage = $"Время UTC: {utcNow.Hours:00}:{utcNow.Minutes:00}{Environment.NewLine}" +
                                      $"Локальное время (GMT+3): {gmtNow.Hours:00}:{gmtNow.Minutes:00}";

                await client.SendMessageAsync(chatId, responseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании ответа о текущем времени.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при вычислении времени.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task SetSubgroupCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo == null)
            {
                await client.SendMessageAsync(chatId, "Вы не зарегистрированы. Используйте /start.", BotKeyboard.Remove);
                return;
            }

            // Проверяем, есть ли уже подгруппа
            if (studentInfo.Subgroup.HasValue)
            {
                await client.SendMessageAsync(chatId, $"Вы уже состоите в {(studentInfo.Subgroup.Value == Subgroup.First ? "1-й" : "2-й")} подгруппе. Используйте /remove_subgroup, чтобы покинуть её.", await CreateMainMenuAsync(messenger, userId));
                return;
            }

            // Предлагаем выбрать подгруппу
            var markup = BotKeyboard.InlineRow(
                new BotButton("1-я подгруппа", "setsubgroup_1"),
                new BotButton("2-я подгруппа", "setsubgroup_2"));
            await client.SendMessageAsync(chatId, "Выберите подгруппу, к которой хотите присоединиться:", markup);
        }

        private async Task RemoveSubgroupCommandAsync(IMessengerClient client, BotMessage message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo == null)
            {
                await client.SendMessageAsync(chatId, "Вы не зарегистрированы. Используйте /start.", BotKeyboard.Remove);
                return;
            }

            // Проверяем, есть ли подгруппа для удаления
            if (!studentInfo.Subgroup.HasValue)
            {
                await client.SendMessageAsync(chatId, "Вы не состоите ни в одной подгруппе.", await CreateMainMenuAsync(messenger, userId));
                return;
            }

            try
            {
                await studentRepo.UpdateStudentSubgroupAsync(messenger, userId, null); // Устанавливаем подгруппу в null
                await client.SendMessageAsync(chatId, "Вы покинули подгруппу. Теперь вы будете получать уведомления о парах обеих подгрупп.", await CreateMainMenuAsync(messenger, userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления подгруппы для пользователя {UserId}", userId);
                await client.SendMessageAsync(chatId, "Ошибка при удалении подгруппы.");
            }
        }

        private async Task HandleSubgroupChoiceAsync(IMessengerClient client, BotCallback callback, string userId, string callbackData, IStudentRepository studentRepo)
        {
            var chatId = callback.ChatId;
            var messageId = callback.MessageId;
            var messenger = client.Messenger;
            Subgroup? selectedSubgroup = null;
            string subgroupDisplay = "";
            if (callbackData == "setsubgroup_1")
            {
                selectedSubgroup = Subgroup.First;
                subgroupDisplay = "1-я";
            }
            else if (callbackData == "setsubgroup_2")
            {
                selectedSubgroup = Subgroup.Second;
                subgroupDisplay = "2-я";
            }
            else
            {
                await client.AnswerCallbackAsync(callback, "Ошибка кнопки", showAlert: true);
                return;
            }

            var studentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (studentInfo == null)
            {
                await client.AnswerCallbackAsync(callback, "Ошибка: вы не зарегистрированы.", showAlert: true);
                return;
            }

            // Проверка: если у студента уже есть подгруппа, не меняем
            if (studentInfo.Subgroup.HasValue)
            {
                await client.AnswerCallbackAsync(callback, "Ошибка: вы уже состоите в подгруппе.", showAlert: true);
                return;
            }

            try
            {
                await studentRepo.UpdateStudentSubgroupAsync(messenger, userId, selectedSubgroup);
                // Убираем клавиатуру с предыдущего сообщения
                await SafeRemoveKeyboardAsync(client, chatId, messageId, callback.MessageText);
                // Отправляем новое сообщение с подтверждением
                await client.SendMessageAsync(chatId, $"Вы присоединились к {subgroupDisplay} подгруппе.", await CreateMainMenuAsync(messenger, userId));
                await client.AnswerCallbackAsync(callback, $"Подгруппа {subgroupDisplay} выбрана.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления подгруппы для пользователя {UserId}", userId);
                await client.SendMessageAsync(chatId, "Ошибка сохранения подгруппы.");
                await client.AnswerCallbackAsync(callback, "Ошибка сохранения", showAlert: true);
            }
        }

        private async Task HandleAttendanceAsync(IMessengerClient client, BotCallback callback, string userId, string callbackData, IStudentRepository studentRepo, IScheduleRepository scheduleRepo, IAttendanceRepository attendanceRepo)
        {
            var chatId = callback.ChatId;
            var messageId = callback.MessageId;
            var messenger = client.Messenger;

            int studentDbId, scheduleId;
            try
            {
                var parts = callbackData.Split('_', 3);
                if (parts.Length < 3 || !int.TryParse(parts[1], out studentDbId) || !int.TryParse(parts[2], out scheduleId))
                {
                    throw new ArgumentException("Invalid callback data format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка парсинга callback attend_: {CallbackData}", callbackData);
                await client.AnswerCallbackAsync(callback, "Ошибка кнопки", showAlert: true);
                return;
            }

            var currentStudentDbId = (await studentRepo.GetByMessengerIdAsync(messenger, userId))?.Id;
            if (currentStudentDbId != studentDbId)
            {
                await client.AnswerCallbackAsync(callback, "Это кнопка для другого студента.", showAlert: true);
                return;
            }

            if (!CanMarkAttendance())
            {
                await client.AnswerCallbackAsync(callback, $"Время для отметки (до {_timeLimitHourUtc.Hours:00}:{_timeLimitHourUtc.Minutes:00} по UTC) уже вышло.", showAlert: true);
                await SafeRemoveKeyboardAsync(client, chatId, messageId, callback.MessageText);
                return;
            }

            var scheduleItem = await scheduleRepo.GetByIdAsync(scheduleId);
            if (scheduleItem == null)
            {
                await client.AnswerCallbackAsync(callback, "Ошибка получения предмета.", showAlert: true);
                return;
            }

            // Проверяем, есть ли занятие сегодня
            var today = DateOnly.FromDateTime(DateTime.Today);
            bool isScheduledToday = false;

            // Проверяем по конкретной дате
            if (scheduleItem.Date.HasValue && scheduleItem.Date.Value == today)
            {
                isScheduledToday = true;
            }
            // Проверяем по дням недели и типу недели
            else if (scheduleItem.DayOfWeek.HasValue)
            {
                var dayOfWeek = scheduleItem.DayOfWeek.Value;
                if (today.DayOfWeek == dayOfWeek)
                {
                    if (scheduleItem.WeekType.HasValue)
                    {
                        var weekNumber = ISOWeek.GetWeekOfYear(DateTime.UtcNow);
                        var isEvenWeek = weekNumber % 2 == 0;

                        if ((scheduleItem.WeekType.Value == WeekType.Even && isEvenWeek) ||
                            (scheduleItem.WeekType.Value == WeekType.Odd && !isEvenWeek))
                        {
                            isScheduledToday = true;
                        }
                    }
                    else
                    {
                        // Если тип недели не указан, считаем, что занятие каждую неделю
                        isScheduledToday = true;
                    }
                }
            }

            if (!isScheduledToday)
            {
                // Удаляем кнопку и уведомляем пользователя
                try
                {
                    var messageText = callback.MessageText;
                    if (string.IsNullOrEmpty(messageText))
                    {
                        await SafeRemoveKeyboardAsync(client, chatId, messageId, messageText);
                    }
                    else
                    {
                        var newText = messageText;
                        var clickButtonText = messageText.Split('\n').LastOrDefault();
                        if (!string.IsNullOrEmpty(clickButtonText))
                        {
                            newText = newText.Replace(clickButtonText, string.Empty);
                        }
                        await client.EditMessageAsync(chatId, messageId, EnsureNotEmpty(newText, messageText));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось убрать кнопку attend_, так как занятия сегодня нет.");
                }

                await client.AnswerCallbackAsync(callback, "Занятия нет в расписании на сегодня.", showAlert: true);
                return;
            }

            try
            {
                // Используем UTC время для отметки
                var utcNow = DateTime.UtcNow;
                // Используем репозиторий для отметки
                await attendanceRepo.MarkAttendanceAsync(studentDbId, scheduleId, utcNow);

                var newText = BuildAttendanceMarkedText(callback.MessageText, "✅ Вы отмечены.");

                try
                {
                    await client.EditMessageAsync(chatId, messageId, newText);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка изменения сообщения attend_");
                    await client.SendMessageAsync(chatId, $"✅ Вы отмечены на занятии '{scheduleItem.Subject.Name}'.");
                }

                await client.AnswerCallbackAsync(callback, "Вы отмечены!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки посещаемости attend_ для студента {StudentId}, предмет {ScheduleId}", studentDbId, scheduleId);
                await client.SendMessageAsync(chatId, "Ошибка при отметке.");
                await client.AnswerCallbackAsync(callback, "Ошибка записи", showAlert: true);
            }
        }

        private async Task HandleMarkOtherAttendanceAsync(IMessengerClient client, BotCallback callback, string userId, string callbackData, IStudentRepository studentRepo, IScheduleRepository scheduleRepo, IAttendanceRepository attendanceRepo)
        {
            var chatId = callback.ChatId;
            var messageId = callback.MessageId;
            var messenger = client.Messenger;

            int studentDbId, scheduleId;
            try
            {
                var parts = callbackData.Split('_', 3);
                if (parts.Length < 3 || !int.TryParse(parts[1], out studentDbId) || !int.TryParse(parts[2], out scheduleId))
                {
                    throw new ArgumentException("Invalid callback data format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка парсинга callback markother_: {CallbackData}", callbackData);
                await client.AnswerCallbackAsync(callback, "Ошибка кнопки", showAlert: true);
                return;
            }

            var currentStudentInfo = await studentRepo.GetByMessengerIdAsync(messenger, userId);
            if (currentStudentInfo == null || currentStudentInfo.Id != studentDbId)
            {
                await client.AnswerCallbackAsync(callback, "Действие не разрешено.", showAlert: true);
                return;
            }

            if (!CanMarkAttendance())
            {
                await client.AnswerCallbackAsync(callback, $"Время для отметки (до {_timeLimitHourUtc.Hours:00}:{_timeLimitHourUtc.Minutes:00} по UTC) уже вышло.", showAlert: true);
                await SafeRemoveKeyboardAsync(client, chatId, messageId, callback.MessageText);
                return;
            }

            var scheduleItem = await scheduleRepo.GetByIdAsync(scheduleId);
            if (scheduleItem == null)
            {
                await client.AnswerCallbackAsync(callback, "Ошибка получения предмета.", showAlert: true);
                return;
            }

            try
            {
                // Используем UTC время для отметки
                var utcNow = DateTime.UtcNow;
                // Используем репозиторий для отметки
                await attendanceRepo.MarkAttendanceAsync(studentDbId, scheduleId, utcNow);

                var markedLine = $"✅ Вы отмечены на занятии '{scheduleItem.Subject.Name}'.";
                var newText = string.IsNullOrEmpty(callback.MessageText)
                    ? markedLine
                    : callback.MessageText + $"\n{markedLine}";

                try
                {
                    await client.EditMessageAsync(chatId, messageId, newText, markdown: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка изменения сообщения markother_");
                    await client.SendMessageAsync(chatId, markedLine);
                }

                await client.AnswerCallbackAsync(callback, $"Вы отмечены на '{scheduleItem.Subject.Name}'!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки посещаемости markother_ для студента {StudentId}, предмет {ScheduleId}", studentDbId, scheduleId);
                await client.SendMessageAsync(chatId, $"Ошибка при отметке на '{scheduleItem.Subject.Name}'.");
                await client.AnswerCallbackAsync(callback, "Ошибка записи", showAlert: true);
            }
        }

        private bool CanMarkAttendance()
        {
            var nowUtc = DateTime.UtcNow;
            // Сравниваем время дня с _timeLimitHourUtc
            return nowUtc.TimeOfDay < _timeLimitHourUtc;
        }

        private bool IsWaitingForLeaderInput(MessengerType messenger, string userId)
        {
            return WaitingLeaders.Contains((messenger, userId)) || WaitingGroupListLeaders.Contains((messenger, userId));
        }

        private async Task ParseScheduleCommandAsync(IMessengerClient client, BotMessage message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            try
            {
                if (IsWaitingForLeaderInput(messenger, userId))
                {
                    await client.SendMessageAsync(chatId, "Бот уже ожидает данные от вас.", BotKeyboard.Remove);
                    return;
                }

                var isLeader = await leaderService.IsLeaderAsync(messenger, userId);

                // Проверка, является ли пользователь старостой
                if (!isLeader)
                {
                    await HandleOtherTextAsync(client, message, userId, studentRepo);
                    return;
                }

                WaitingLeaders.Add((messenger, userId));
                await client.SendMessageAsync(chatId, "Готов принять новое расписание. Отправьте его от бота @knrtukaibot.", BotKeyboard.Remove);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /parse_schedule.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при обработке команды.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task ProcessScheduleTextAsync(IMessengerClient client, BotMessage message, string userId)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;

            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    await client.SendMessageAsync(chatId, "Полученное сообщение не содержит текста расписания.");
                    return;
                }

                var scheduleData = ScheduleParser.ParseScheduleText(message.Text, _logger);
                if (scheduleData == null || !scheduleData.Success)
                {
                    await client.SendMessageAsync(chatId, "Ошибка при разборе формата расписания. Проверьте формат и повторите.");
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                var subjectRepo = scope.ServiceProvider.GetRequiredService<ISubjectRepository>();

                var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                // Удаляем старое расписание
                await subjectRepo.DeleteAllAsync();
                var scheduleIds = await scheduleRepo.DeleteAllAsync();

                if (scheduleIds.Any())
                {
                    var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                    var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                    await attendanceRepo.DeleteAttendanceByScheduleIdsAsync(scheduleIds);
                    await notificationRepo.DeleteNotifByScheduleIdsAsync(scheduleIds);
                }
                await transaction.CommitAsync();

                // Сохраняем новое расписание
                foreach (var item in scheduleData.Data)
                {
                    // Найдем или создадим предмет
                    var subject = await subjectRepo.GetByNameAsync(item.SubjectName);
                    if (subject == null)
                    {
                        subject = new Subject { Name = item.SubjectName };
                        await subjectRepo.AddAsync(subject);
                        await subjectRepo.SaveChangesAsync(); // Убедитесь, что Id сгенерирован
                    }

                    var autoMapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                    var newSchedule = autoMapper.Map<Schedule>(item);
                    newSchedule.SubjectId = subject.Id;

                    if (item.Dates != null && item.Dates.Count != 0)
                    {
                        // Если есть конкретные даты, создаем отдельные записи для каждой даты
                        foreach (var specificDate in item.Dates)
                        {
                            // Создаем новый экземпляр Schedule вручную
                            var specificSchedule = new Schedule
                            {
                                // Копируем все свойства из newSchedule
                                SubjectId = newSchedule.SubjectId,
                                StartTime = newSchedule.StartTime,
                                ClassType = newSchedule.ClassType,
                                Room = newSchedule.Room,
                                Building = newSchedule.Building,
                                // Дату берем из item.Dates
                                Date = specificDate,
                                // Дни недели и тип недели должны быть null для конкретной даты
                                DayOfWeek = null,
                                WeekType = null,
                                Subgroup = newSchedule.Subgroup,
                            };
                            await scheduleRepo.AddAsync(specificSchedule);
                        }
                    }
                    else
                    {
                        // Иначе сохраняем как обычно
                        await scheduleRepo.AddAsync(newSchedule);
                    }
                }

                await scheduleRepo.SaveChangesAsync();
                await client.SendMessageAsync(chatId, "Расписание успешно обновлено.", await CreateMainMenuAsync(messenger, userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге или сохранении расписания.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при обработке расписания.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task ParseGroupListCommandAsync(IMessengerClient client, BotMessage message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            try
            {
                if (IsWaitingForLeaderInput(messenger, userId))
                {
                    await client.SendMessageAsync(chatId, "Бот уже ожидает данные от вас.", BotKeyboard.Remove);
                    return;
                }

                var isLeader = await leaderService.IsLeaderAsync(messenger, userId);
                if (!isLeader)
                {
                    await HandleOtherTextAsync(client, message, userId, studentRepo);
                    return;
                }

                WaitingGroupListLeaders.Add((messenger, userId));
                await client.SendMessageAsync(chatId,
                    $"Готов принять новый список группы.{Environment.NewLine}{Environment.NewLine}{GroupListFormatExample}",
                    BotKeyboard.Remove);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /parse_group_list.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при обработке команды.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task ProcessGroupListTextAsync(IMessengerClient client, BotMessage message, string userId)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;

            try
            {
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    await client.SendMessageAsync(chatId,
                        $"Полученное сообщение не содержит списка группы. Отправьте /parse_group_list ещё раз." +
                        $"{Environment.NewLine}{Environment.NewLine}{GroupListFormatExample}");
                    return;
                }

                var parsedGroupList = GroupListParser.ParseGroupListText(message.Text, _logger);
                if (parsedGroupList == null || !parsedGroupList.Success)
                {
                    var errorMessages = parsedGroupList?.Errors
                        .Select(error => error.Message)
                        .Where(errorMessage => !string.IsNullOrWhiteSpace(errorMessage))
                        .ToArray() ?? [];

                    var details = errorMessages.Length == 0
                        ? string.Empty
                        : $"{Environment.NewLine}{string.Join(Environment.NewLine, errorMessages)}";

                    await client.SendMessageAsync(chatId,
                        $"Ошибка при разборе списка группы. Проверьте формат и отправьте /parse_group_list ещё раз." +
                        $"{details}{Environment.NewLine}{Environment.NewLine}{GroupListFormatExample}");
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
                var leaderRepo = scope.ServiceProvider.GetRequiredService<ILeaderRepository>();
                var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                var leaderService = scope.ServiceProvider.GetRequiredService<ILeaderService>();

                if (!await leaderService.IsLeaderAsync(messenger, userId))
                {
                    await client.SendMessageAsync(chatId, "Команда доступна только старосте.", await CreateMainMenuAsync(messenger, userId));
                    return;
                }

                var newStudents = parsedGroupList.Data;

                // При первом развёртывании студентов в базе ещё нет, поэтому староста из конфига списка
                // группы может не быть зарегистрированным студентом — тогда искать его в новом списке нечем
                var currentLeaderStudent = await studentRepo.GetByMessengerIdAsync(messenger, userId);
                Student? currentLeaderInNewList = null;

                if (currentLeaderStudent != null)
                {
                    // Староста уже зарегистрирован: не даём ему случайно вычеркнуть себя из группы
                    currentLeaderInNewList = FindMatchingStudent(newStudents, currentLeaderStudent);
                    if (currentLeaderInNewList == null)
                    {
                        await client.SendMessageAsync(chatId,
                            $"Не нашёл вас в новом списке группы по ФИО или номеру. Список не обновлён.{Environment.NewLine}{Environment.NewLine}{GroupListFormatExample}",
                            await CreateMainMenuAsync(messenger, userId));
                        return;
                    }
                }

                var leaderStudents = await leaderRepo.GetLeaderStudentsAsync();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                var archivedStudents = (await studentRepo.DeleteAllAsync()).ToList();
                var archivedStudentIds = archivedStudents.Select(student => student.Id).ToArray();

                if (archivedStudentIds.Length != 0)
                {
                    await attendanceRepo.DeleteAttendanceByStudentIdsAsync(archivedStudentIds);
                    await notificationRepo.DeleteNotifByStudentIdsAsync(archivedStudentIds);
                    await leaderRepo.DeleteLeaderByStudentIdsAsync(archivedStudentIds);
                }

                PreserveStudentRegistrations(newStudents, archivedStudents);

                if (currentLeaderInNewList != null && currentLeaderStudent != null)
                {
                    currentLeaderInNewList.TelegramId = currentLeaderStudent.TelegramId;
                    currentLeaderInNewList.VkId = currentLeaderStudent.VkId;
                    currentLeaderInNewList.Subgroup = currentLeaderStudent.Subgroup;
                }

                foreach (var newStudent in newStudents)
                {
                    await studentRepo.AddAsync(newStudent);
                }

                await studentRepo.SaveChangesAsync();

                var leaderStudentIds = ResolveLeaderStudentIds(newStudents, leaderStudents);
                if (currentLeaderInNewList != null)
                    leaderStudentIds = leaderStudentIds.Append(currentLeaderInNewList.Id);

                foreach (var leaderStudentId in leaderStudentIds.Distinct().ToArray())
                {
                    await leaderRepo.AddAsync(new Leader { StudentId = leaderStudentId });
                }

                await leaderRepo.SaveChangesAsync();

                // Основные старосты заданы айди мессенджеров в конфиге, после смены списка права им нужно вернуть
                await leaderService.SyncMainLeadersAsync();

                await transaction.CommitAsync();

                var preservedRegistrationsCount = newStudents.Count(student => student.HasAnyMessenger());

                // Староста из конфига после первой загрузки списка ещё не привязан к студенту
                var registrationHint = currentLeaderStudent == null
                    ? $"{Environment.NewLine}Вы ещё не зарегистрированы в боте — отправьте /start и выберите свой номер в списке."
                    : string.Empty;

                await client.SendMessageAsync(chatId,
                    $"Список группы успешно обновлён. Загружено студентов: {newStudents.Count}. " +
                    $"Сохранено регистраций: {preservedRegistrationsCount}.{registrationHint}",
                    await CreateMainMenuAsync(messenger, userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге или сохранении списка группы.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при обработке списка группы.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        /// <summary>
        /// Проверяет, что группа делится на подгруппы. Если деления нет, команды подгрупп вызывать нельзя
        /// </summary>
        private async Task<bool> EnsureSubgroupsEnabledAsync(IMessengerClient client, BotMessage message, string userId, IGroupSettingsRepository groupSettingsRepo)
        {
            if (await groupSettingsRepo.HasSubgroupsAsync())
                return true;

            await client.SendMessageAsync(message.ChatId,
                "Группа не делится на подгруппы, поэтому команда недоступна. Изменить это может староста командой /subgroups.",
                await CreateMainMenuAsync(client.Messenger, userId));

            return false;
        }

        private async Task SubgroupsSettingCommandAsync(IMessengerClient client, BotMessage message, string userId, ILeaderService leaderService,
            IGroupSettingsRepository groupSettingsRepo, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            try
            {
                if (!await leaderService.IsLeaderAsync(messenger, userId))
                {
                    await HandleOtherTextAsync(client, message, userId, studentRepo);
                    return;
                }

                var hasSubgroups = await groupSettingsRepo.HasSubgroupsAsync();
                var markup = BotKeyboard.InlineRow(
                    new BotButton("Делится на подгруппы", $"{SubgroupsSettingCallbackPrefix}1"),
                    new BotButton("Не делится", $"{SubgroupsSettingCallbackPrefix}0"));

                await client.SendMessageAsync(chatId,
                    $"Сейчас группа {(hasSubgroups ? "делится на подгруппы" : "не делится на подгруппы")}." +
                    $"{Environment.NewLine}Выберите, как должно быть:",
                    markup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /subgroups.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при обработке команды.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task HandleSubgroupsSettingChoiceAsync(IMessengerClient client, BotCallback callback, string userId, string callbackData,
            ILeaderService leaderService, IGroupSettingsRepository groupSettingsRepo, IStudentRepository studentRepo)
        {
            var chatId = callback.ChatId;
            var messageId = callback.MessageId;
            var messenger = client.Messenger;

            if (!await leaderService.IsLeaderAsync(messenger, userId))
            {
                await client.AnswerCallbackAsync(callback, "Команда доступна только старосте.", showAlert: true);
                return;
            }

            if (callbackData != $"{SubgroupsSettingCallbackPrefix}1" && callbackData != $"{SubgroupsSettingCallbackPrefix}0")
            {
                await client.AnswerCallbackAsync(callback, "Ошибка кнопки", showAlert: true);
                return;
            }

            var hasSubgroups = callbackData == $"{SubgroupsSettingCallbackPrefix}1";

            try
            {
                await groupSettingsRepo.SetHasSubgroupsAsync(hasSubgroups);

                // Без деления группы подгруппы студентов теряют смысл, поэтому очищаем их
                if (!hasSubgroups)
                    await studentRepo.ResetSubgroupsAsync();

                await SafeRemoveKeyboardAsync(client, chatId, messageId, callback.MessageText);
                await client.SendMessageAsync(chatId,
                    hasSubgroups
                        ? "Группа делится на подгруппы. Студентам доступны /set_subgroup, /remove_subgroup и /attend_other_group."
                        : "Группа не делится на подгруппы. Команды работы с подгруппами скрыты, подгруппы студентов сброшены.",
                    await CreateMainMenuAsync(messenger, userId));
                await client.AnswerCallbackAsync(callback, "Настройка сохранена.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения настройки деления группы на подгруппы.");
                await client.SendMessageAsync(chatId, "Ошибка сохранения настройки.");
                await client.AnswerCallbackAsync(callback, "Ошибка сохранения", showAlert: true);
            }
        }

        private async Task AddLeaderCommandAsync(IMessengerClient client, BotMessage message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            try
            {
                if (!await leaderService.IsLeaderAsync(messenger, userId))
                {
                    await HandleOtherTextAsync(client, message, userId, studentRepo);
                    return;
                }

                var candidates = await leaderService.GetAssistantCandidatesAsync();
                if (candidates.Count == 0)
                {
                    await client.SendMessageAsync(chatId,
                        "Назначать некого: все зарегистрированные в боте студенты уже имеют права старосты.",
                        await CreateMainMenuAsync(messenger, userId));
                    return;
                }

                var leaders = await leaderService.GetLeadersAsync();
                await client.SendMessageAsync(chatId,
                    $"{FormatLeaders(leaders)}{Environment.NewLine}Выберите, кого назначить помощником старосты:",
                    CreateStudentsMarkup(candidates, AddLeaderCallbackPrefix));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /add_leader.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при обработке команды.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private async Task RemoveLeaderCommandAsync(IMessengerClient client, BotMessage message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.ChatId;
            var messenger = client.Messenger;
            try
            {
                if (!await leaderService.IsLeaderAsync(messenger, userId))
                {
                    await HandleOtherTextAsync(client, message, userId, studentRepo);
                    return;
                }

                var assistants = await leaderService.GetRemovableAssistantsAsync();
                if (assistants.Count == 0)
                {
                    await client.SendMessageAsync(chatId,
                        "Снимать некого: помощников старосты сейчас нет. Основного старосту можно сменить только через конфиг.",
                        await CreateMainMenuAsync(messenger, userId));
                    return;
                }

                await client.SendMessageAsync(chatId, "Выберите, с кого снять права помощника старосты:",
                    CreateStudentsMarkup(assistants, RemoveLeaderCallbackPrefix));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /remove_leader.");
                await client.SendMessageAsync(chatId, "Произошла ошибка при обработке команды.", await CreateMainMenuAsync(messenger, userId));
            }
        }

        private Task HandleAddLeaderChoiceAsync(IMessengerClient client, BotCallback callback, string userId, string callbackData, ILeaderService leaderService)
        {
            return HandleLeaderChangeAsync(client, callback, userId, callbackData, AddLeaderCallbackPrefix, leaderService,
                leaderService.AddAssistantAsync,
                student => $"{student.GetFullName()} назначен помощником старосты.",
                "Вам выданы права помощника старосты. Список доступных команд — /help.");
        }

        private Task HandleRemoveLeaderChoiceAsync(IMessengerClient client, BotCallback callback, string userId, string callbackData, ILeaderService leaderService)
        {
            return HandleLeaderChangeAsync(client, callback, userId, callbackData, RemoveLeaderCallbackPrefix, leaderService,
                leaderService.RemoveAssistantAsync,
                student => $"С {student.GetFullName()} сняты права помощника старосты.",
                "С вас сняты права помощника старосты.");
        }

        /// <summary>
        /// Общая обработка кнопок назначения и снятия помощника старосты
        /// </summary>
        private async Task HandleLeaderChangeAsync(IMessengerClient client, BotCallback callback, string userId, string callbackData, string callbackPrefix,
            ILeaderService leaderService, Func<int, Task<IDataResult<Student>>> changeLeaderAsync,
            Func<Student, string> formatResultForLeader, string textForStudent)
        {
            var chatId = callback.ChatId;
            var messageId = callback.MessageId;
            var messenger = client.Messenger;

            if (!await leaderService.IsLeaderAsync(messenger, userId))
            {
                await client.AnswerCallbackAsync(callback, "Команда доступна только старосте.", showAlert: true);
                return;
            }

            if (!int.TryParse(callbackData[callbackPrefix.Length..], out var studentId))
            {
                _logger.LogError("Ошибка парсинга callback {CallbackPrefix}: {CallbackData}", callbackPrefix, callbackData);
                await client.AnswerCallbackAsync(callback, "Ошибка кнопки", showAlert: true);
                return;
            }

            try
            {
                var changeResult = await changeLeaderAsync(studentId);

                // Кнопки одноразовые: список старост уже изменился
                await SafeRemoveKeyboardAsync(client, chatId, messageId, callback.MessageText);

                if (!changeResult.Success)
                {
                    var errorText = changeResult.Errors[0].Message;
                    await client.SendMessageAsync(chatId, errorText, await CreateMainMenuAsync(messenger, userId));
                    await client.AnswerCallbackAsync(callback, errorText, showAlert: true);
                    return;
                }

                await client.SendMessageAsync(chatId, formatResultForLeader(changeResult.Data), await CreateMainMenuAsync(messenger, userId));
                await client.AnswerCallbackAsync(callback, "Готово.");

                await NotifyStudentAboutLeaderRightsAsync(changeResult.Data, textForStudent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка изменения списка старост. CallbackData={CallbackData}", callbackData);
                await client.SendMessageAsync(chatId, "Произошла ошибка при изменении списка старост.");
                await client.AnswerCallbackAsync(callback, "Ошибка сохранения", showAlert: true);
            }
        }

        /// <summary>
        /// Сообщает студенту об изменении прав, заодно обновляя ему меню.
        /// Пишем во все мессенджеры, которые студент привязал
        /// </summary>
        private async Task NotifyStudentAboutLeaderRightsAsync(Student student, string text)
        {
            foreach (var client in _clients.Where(candidate => candidate.IsEnabled))
            {
                var messengerId = student.GetMessengerId(client.Messenger);
                if (string.IsNullOrWhiteSpace(messengerId))
                    continue;

                try
                {
                    await client.SendMessageAsync(messengerId, text, await CreateMainMenuAsync(client.Messenger, messengerId));
                }
                catch (Exception ex)
                {
                    // Студент мог заблокировать бота, для смены прав это не критично
                    _logger.LogWarning(ex, "Не удалось уведомить студента {StudentId} об изменении прав старосты в {Messenger}.",
                        student.Id, client.Messenger);
                }
            }
        }

        private static string FormatLeaders(IReadOnlyCollection<Student> leaders)
        {
            if (leaders.Count == 0)
                return $"Сейчас права старосты не выданы никому.{Environment.NewLine}";

            var leaderLines = leaders.Select(leader => $"• {leader.GetFullName()}");

            return $"Сейчас права старосты есть у:{Environment.NewLine}{string.Join(Environment.NewLine, leaderLines)}{Environment.NewLine}";
        }

        private static BotKeyboard CreateStudentsMarkup(IEnumerable<Student> students, string callbackPrefix)
        {
            return BotKeyboard.InlineColumn(students.Select(student =>
                new BotButton($"{student.NumberInGroup}. {student.GetFullName()}", $"{callbackPrefix}{student.Id}")));
        }

        private static string FormatSchedule(IEnumerable<Schedule> scheduleData)
        {
            if (!scheduleData.Any())
            {
                return "Сегодня пар нет.";
            }

            var scheduleList = scheduleData.Select(s => $"• {s.StartTime:hh\\:mm} - {s.Subject.Name}");
            return string.Join(Environment.NewLine, scheduleList);
        }

        /// <summary>
        /// Переносит привязки мессенджеров и подгруппу на студентов нового списка группы
        /// </summary>
        private static void PreserveStudentRegistrations(IReadOnlyCollection<Student> newStudents, IEnumerable<Student> archivedStudents)
        {
            var registeredStudentsByFullName = archivedStudents
                .Where(student => student.HasAnyMessenger())
                .GroupBy(NormalizeFullName)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single());

            foreach (var newStudent in newStudents)
            {
                if (!registeredStudentsByFullName.TryGetValue(NormalizeFullName(newStudent), out var archivedStudent))
                    continue;

                newStudent.TelegramId = archivedStudent.TelegramId;
                newStudent.VkId = archivedStudent.VkId;
                newStudent.Subgroup = archivedStudent.Subgroup;
            }
        }

        private static IEnumerable<int> ResolveLeaderStudentIds(IEnumerable<Student> newStudents, IEnumerable<Student> archivedLeaderStudents)
        {
            foreach (var archivedLeaderStudent in archivedLeaderStudents)
            {
                var newLeaderStudent = FindMatchingStudent(newStudents, archivedLeaderStudent);
                if (newLeaderStudent != null)
                {
                    yield return newLeaderStudent.Id;
                }
            }
        }

        private static Student? FindMatchingStudent(IEnumerable<Student> students, Student targetStudent)
        {
            var targetFullName = NormalizeFullName(targetStudent);

            return students.FirstOrDefault(student => NormalizeFullName(student) == targetFullName)
                ?? students.FirstOrDefault(student => student.NumberInGroup == targetStudent.NumberInGroup);
        }

        private static string NormalizeFullName(Student student)
        {
            return string.Join(' ', new[] { student.SurName, student.Name, student.FatherName }
                    .Where(part => !string.IsNullOrWhiteSpace(part)))
                .Trim()
                .ToLowerInvariant();
        }

        /// <summary>
        /// Собирает текст сообщения после отметки: шапка уведомления плюс строка об отметке.
        /// Текст исходного сообщения приходит не из всех мессенджеров, поэтому его может не быть
        /// </summary>
        private static string BuildAttendanceMarkedText(string messageText, string markedLine)
        {
            if (string.IsNullOrEmpty(messageText))
                return markedLine;

            var lines = messageText.Split('\n');
            var header = lines.Length > 1
                ? lines[0] + Environment.NewLine + lines[1]
                : lines[0];

            return $"{header}\n{markedLine}";
        }

        private static string EnsureNotEmpty(string text, string fallback)
        {
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        /// <summary>
        /// Убирает кнопки, не давая упасть обработке, если сообщение уже нельзя изменить
        /// </summary>
        private async Task SafeRemoveKeyboardAsync(IMessengerClient client, string chatId, string messageId, string? fallbackText)
        {
            try
            {
                await client.RemoveKeyboardAsync(chatId, messageId, fallbackText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось убрать кнопки у сообщения {MessageId} в {Messenger}.", messageId, client.Messenger);
            }
        }

        private async Task SafeSendAsync(IMessengerClient client, string chatId, string text, BotKeyboard? keyboard)
        {
            try
            {
                await client.SendMessageAsync(chatId, text, keyboard);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить сообщение в {Messenger}, чат {ChatId}.", client.Messenger, chatId);
            }
        }

        private async Task SafeAnswerAsync(IMessengerClient client, BotCallback callback, string text, bool showAlert)
        {
            try
            {
                await client.AnswerCallbackAsync(callback, text, showAlert);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось ответить на нажатие кнопки в {Messenger}.", client.Messenger);
            }
        }

        /// <summary>
        /// Главное меню. Состав кнопок зависит от прав пользователя и от того, делится ли группа на подгруппы
        /// </summary>
        private async Task<BotKeyboard> CreateMainMenuAsync(MessengerType messenger, string? userId = null)
        {
            List<BotButton> commandButtons = [];
            using var scope = _scopeFactory.CreateScope();
            var groupSettingsRepo = scope.ServiceProvider.GetRequiredService<IGroupSettingsRepository>();
            var hasSubgroups = await groupSettingsRepo.HasSubgroupsAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                var leaderService = scope.ServiceProvider.GetRequiredService<ILeaderService>();
                var isLeader = await leaderService.IsLeaderAsync(messenger, userId);
                if (isLeader)
                {
                    commandButtons.Add(new("/list"));
                    commandButtons.Add(new("/parse_schedule"));
                    commandButtons.Add(new("/parse_group_list"));
                    commandButtons.Add(new("/subgroups"));
                    commandButtons.Add(new("/add_leader"));
                    commandButtons.Add(new("/remove_leader"));
                }

                if (leaderService.IsAdmin(messenger, userId))
                {
                    commandButtons.Add(new("/time"));
                }
            }

            var menuRows = new List<BotButton[]>();
            menuRows.Add([new("/schedule"), new("/info")]);

            // Кнопку отметки за другую подгруппу показываем, только когда группа делится на подгруппы
            if (hasSubgroups)
                menuRows.Add([new("/attend_other_group"), new("/help")]);
            else
                menuRows.Add([new("/help")]);

            // Команд старосты много, раскладываем их по две в ряд, чтобы клавиатура не расползалась
            menuRows.AddRange(commandButtons.Chunk(2));

            return BotKeyboard.Reply(menuRows);
        }
    }
}
