using AutoMapper;
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
using GroupListNet.Core.src.Services.Impl;
using GroupListNet.Utils.src.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Schema;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GroupListNet.Core.src.BackgroundJobs
{
    public class TelegramBotBackgroundJob : BackgroundService
    {
        private readonly ILogger<TelegramBotBackgroundJob> _logger;
        private TelegramBotClient _botClient { get; set; }
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string TelegramBotToken;
        private readonly TimeSpan _timeLimitHourUtc;
        private readonly TimeSpan _notificationCheckInterval = TimeSpan.FromMinutes(1);
        private readonly TelegramSettingsConfiguration TelegramConfig;
        /// <summary>
        /// Телеграмм айди старост, которые должны отправить расписание
        /// </summary>
        private readonly List<string> WaitingLeaders = [];
        /// <summary>
        /// Телеграмм айди старост, которые должны отправить список группы
        /// </summary>
        private readonly List<string> WaitingGroupListLeaders = [];

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

        public TelegramBotBackgroundJob(ILogger<TelegramBotBackgroundJob> logger,
             IConfiguration config,
              IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            TelegramConfig = config.GetSection(TelegramSettingsConfiguration.TelegramSectionInConfig).Get<TelegramSettingsConfiguration>()
               ?? throw new InvalidOperationException($"Cannot get {TelegramSettingsConfiguration.TelegramSectionInConfig} section from config. " +
               $"Value is null.");

            TelegramBotToken = TelegramConfig.TelegramBotToken;

            var timeLimitHourUtc = 17;
            try
            {
                var timeLimitHourStr = config.GetSection(SendingSettings.SendingSettingsSectionInConfig).Get<SendingSettings>()?.DeadlineTime;
                if (!TimeSpan.TryParse(timeLimitHourStr, CultureInfo.InvariantCulture, out _timeLimitHourUtc))
                {
                    _timeLimitHourUtc = new TimeSpan(timeLimitHourUtc, 0, 0);
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var logNotificatiorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
                        logNotificatiorService.LogAndNotifyAdminsAsync($"Ошибка в {nameof(TelegramBotBackgroundJob)}, " +
                        $"не удалось спарсить DeadlineTime из конфига. Используемое значение = {timeLimitHourUtc}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} error getting value from config! Using default value = {DefaultValue}{NewLine}",
                    nameof(TelegramBotBackgroundJob), timeLimitHourUtc, Environment.NewLine);
            }

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _botClient = new TelegramBotClient(TelegramBotToken, cancellationToken: stoppingToken);
            _botClient.OnUpdate += async (update) =>
            {
                if (update.Message != null)
                {
                    await HandleMessageAsync(update.Message);
                }
                else if (update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync(update.CallbackQuery);
                }
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendScheduledNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "В {ClassName} неизвестная ошибка в цикле отправки уведомлений.{NewLine}",
                        nameof(TelegramBotBackgroundJob), Environment.NewLine);
                }
                finally
                {
                    await Task.Delay(_notificationCheckInterval, stoppingToken); // Ждем установленный интервал перед следующей проверкой
                }
            }
        }

        private async Task HandleMessageAsync(Message message)
        {
            try
            {
                if (message == null || message.Text == null || message.From == null) return;
                var userId = message.From.Id.ToString();
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
                        await StartCommandAsync(message, userId, studentRepo);
                        break;
                    case "/help":
                        await HelpCommandAsync(message, userId, leaderService, groupSettingsRepo);
                        break;
                    case "/info":
                        await InfoCommandAsync(message, userId, studentRepo, groupSettingsRepo);
                        break;
                    case "/schedule":
                        await ScheduleCommandAsync(message, userId, studentRepo, scheduleRepo);
                        break;
                    case "/attend_other_group":
                        if (await EnsureSubgroupsEnabledAsync(message, userId, groupSettingsRepo))
                            await AttendOtherGroupCommandAsync(message, userId, studentRepo, scheduleRepo, attendanceRepo);
                        break;
                    case "/set_subgroup":
                        if (await EnsureSubgroupsEnabledAsync(message, userId, groupSettingsRepo))
                            await SetSubgroupCommandAsync(message, userId, studentRepo);
                        break;
                    case "/remove_subgroup":
                        if (await EnsureSubgroupsEnabledAsync(message, userId, groupSettingsRepo))
                            await RemoveSubgroupCommandAsync(message, userId, studentRepo);
                        break;
                    case "/list": // команда для старосты
                        await ListCommandAsync(message, userId, studentRepo, attendanceRepo, leaderService);
                        break;
                    case "/time": // команда для администратора
                        await TimeCommandAsync(message, userId, studentRepo);
                        break;
                    case "/parse_schedule": // команда для старосты
                        await ParseScheduleCommandAsync(message, userId, leaderService, studentRepo);
                        break;
                    case "/parse_group_list": // команда для старосты
                        await ParseGroupListCommandAsync(message, userId, leaderService, studentRepo);
                        break;
                    case "/subgroups": // команда для старосты
                        await SubgroupsSettingCommandAsync(message, userId, leaderService, groupSettingsRepo, studentRepo);
                        break;
                    case "/add_leader": // команда для старосты
                        await AddLeaderCommandAsync(message, userId, leaderService, studentRepo);
                        break;
                    case "/remove_leader": // команда для старосты
                        await RemoveLeaderCommandAsync(message, userId, leaderService, studentRepo);
                        break;
                    default:
                        if (WaitingLeaders.Contains(userId))
                        {
                            await ProcessScheduleTextAsync(message, userId);
                            WaitingLeaders.Remove(userId);
                        }
                        else if (WaitingGroupListLeaders.Contains(userId))
                        {
                            await ProcessGroupListTextAsync(message, userId);
                            WaitingGroupListLeaders.Remove(userId);
                        }
                        else if (int.TryParse(text, out int number))
                        {
                            await HandleNumberInputAsync(message, userId, number, studentRepo, leaderService);
                        }
                        else
                        {
                            await HandleOtherTextAsync(message, userId, studentRepo);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке текстового сообщения. Ошибка произошла в {ClassName}. " +
                    "chatId={ChatId}. TgUserId={UserTgId}. Text={Text}",
                    nameof(TelegramBotBackgroundJob), message?.Chat?.Id, message?.From?.Id.ToString(), message?.Text);
                if (message != null)
                    await _botClient.SendMessage(message.Chat.Id, "Произошла системная ошибка. Попробуйте ещё раз. " +
                        "Если ошибка повторится, напишите администратору", replyMarkup: await CreateMainMenu());
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var userId = callbackQuery.From.Id.ToString();
            var callbackData = callbackQuery.Data;
            using var scope = _scopeFactory.CreateScope();
            var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
            var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
            var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var leaderService = scope.ServiceProvider.GetRequiredService<ILeaderService>();
            var groupSettingsRepo = scope.ServiceProvider.GetRequiredService<IGroupSettingsRepository>();

            if (callbackData.StartsWith("setsubgroup_"))
            {
                await HandleSubgroupChoiceAsync(callbackQuery, userId, callbackData, studentRepo);
            }
            else if (callbackData.StartsWith("attend_"))
            {
                await HandleAttendanceAsync(callbackQuery, userId, callbackData, studentRepo, scheduleRepo, attendanceRepo);
            }
            else if (callbackData.StartsWith("markother_"))
            {
                await HandleMarkOtherAttendanceAsync(callbackQuery, userId, callbackData, studentRepo, scheduleRepo, attendanceRepo);
            }
            else if (callbackData.StartsWith(SubgroupsSettingCallbackPrefix))
            {
                await HandleSubgroupsSettingChoiceAsync(callbackQuery, userId, callbackData, leaderService, groupSettingsRepo, studentRepo);
            }
            else if (callbackData.StartsWith(AddLeaderCallbackPrefix))
            {
                await HandleAddLeaderChoiceAsync(callbackQuery, userId, callbackData, leaderService);
            }
            else if (callbackData.StartsWith(RemoveLeaderCallbackPrefix))
            {
                await HandleRemoveLeaderChoiceAsync(callbackQuery, userId, callbackData, leaderService);
            }
            else
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Неизвестная команда.", showAlert: true);
            }
        }

        private async Task StartCommandAsync(Message message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo != null)
            {
                await _botClient.SendMessage(chatId, $"С возвращением, {studentInfo.Name}! 👋", replyMarkup: await CreateMainMenu(studentInfo.TelegramId));
            }
            else // Если не зарегистрирован
            {
                var students = await studentRepo.GetStudentsWithoutTelegramAsync();
                if (!students.Any())
                {
                    await _botClient.SendMessage(chatId, "Не удалось загрузить список группы.");
                    return;
                }
                var groupList = string.Join(Environment.NewLine, students.OrderBy(s => s.NumberInGroup).Select(s => $"{ s.NumberInGroup}. {s.SurName} { s.Name} { s.FatherName}"));
                await _botClient.SendMessage(chatId, $"Привет! Список группы:{Environment.NewLine}{groupList}{Environment.NewLine}Введите свой номер: ", replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private async Task HelpCommandAsync(Message message, string? userId, ILeaderService leaderService, IGroupSettingsRepository groupSettingsRepo)
        {
            var chatId = message.Chat.Id;
            try
            {
                var replaceTextForLeader = string.Empty;
                var replaceTextForAdmin = string.Empty;

                if (await leaderService.IsLeaderAsync(userId))
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

                if (userId != null && TelegramConfig.AdminTelegramIdsArray.Contains(userId))
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

                await _botClient.SendMessage(chatId, helpText, replyMarkup: await CreateMainMenu(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании сообщения помощи.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при формировании сообщения. Если ошибка повторится, обратитесь к администратору.", 
                    replyMarkup: await CreateMainMenu(userId));
            }
            
        }

        private async Task InfoCommandAsync(Message message, string userId, IStudentRepository studentRepo, IGroupSettingsRepository groupSettingsRepo)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo == null)
            {
                await _botClient.SendMessage(chatId, "Вы не зарегистрированы. Используйте /start.", replyMarkup: new ReplyKeyboardRemove());
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
            await _botClient.SendMessage(chatId, infoText, ParseMode.Markdown, replyMarkup: await CreateMainMenu(userId));
        }

        private async Task ScheduleCommandAsync(Message message, string userId, IStudentRepository studentRepo, IScheduleRepository scheduleRepo)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo == null)
            {
                await _botClient.SendMessage(chatId, "Вы не зарегистрированы. Используйте /start.", replyMarkup: new ReplyKeyboardRemove());
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
                await _botClient.SendMessage(chatId, header + scheduleText, ParseMode.Markdown, replyMarkup: await CreateMainMenu(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения расписания для студента {StudentId}", studentInfo.Id);
                await _botClient.SendMessage(chatId, "Не удалось получить расписание.", ParseMode.Markdown, replyMarkup: await CreateMainMenu(userId));
            }
        }

        private async Task AttendOtherGroupCommandAsync(Message message, string userId, IStudentRepository studentRepo, IScheduleRepository scheduleRepo, IAttendanceRepository attendanceRepo)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo == null)
            {
                await _botClient.SendMessage(chatId, "Вы не зарегистрированы. Используйте /start.", replyMarkup: new ReplyKeyboardRemove());
                return;
            }

            // Проверка: если у студента нет подгруппы, он не может отмечаться на паре другой подгруппы
            if (!studentInfo.Subgroup.HasValue)
            {
                await _botClient.SendMessage(chatId, "Вы не состоите в подгруппе. Вы можете отмечаться на парах обеих подгрупп, используя кнопки из уведомлений о начале пары.", 
                    replyMarkup: await CreateMainMenu(userId));
                return;
            }

            var currentSubgroup = studentInfo.Subgroup.Value; // Теперь безопасно, так как проверили
            var otherSubgroup = currentSubgroup == Subgroup.First ? Subgroup.Second : Subgroup.First;
            var otherSubgroupDisplay = currentSubgroup == Subgroup.First ? "2-й" : "1-й";

            if (!CanMarkAttendance())
            {
                await _botClient.SendMessage(chatId, $"К сожалению, время для отметки (до {_timeLimitHourUtc.Hours:00}:{_timeLimitHourUtc.Minutes:00} по UTC) уже вышло.", replyMarkup: await CreateMainMenu(userId));
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
                    await _botClient.SendMessage(chatId, $"У **{otherSubgroupDisplay} подгруппы** сегодня ({dateStr}, {dayName}, {(weekType == WeekType.Even ? "четная" : "нечетная")} неделя) пар нет.", replyMarkup: await CreateMainMenu(userId));
                    return;
                }
                var markup = new InlineKeyboardMarkup();
                var scheduleLines = new List<string> { $"Пары **{otherSubgroupDisplay} подгруппы** на сегодня ({dateStr}, {dayName}, {(weekType == WeekType.Even ? "четная" : "нечетная")} неделя):" };
                foreach (var scheduleItem in otherScheduleData)
                {
                    var callbackData = $"markother_{studentInfo.Id}_{scheduleItem.Id}";
                    var button = new InlineKeyboardButton($"{scheduleItem.StartTime} - {scheduleItem.Subject.Name}") { CallbackData = callbackData };
                    markup.InlineKeyboard = markup.InlineKeyboard.Append(new[] { button }).ToArray();
                    scheduleLines.Add($"• {scheduleItem.StartTime} - {scheduleItem.Subject.Name}");
                }
                var scheduleText = string.Join("", scheduleLines);
                await _botClient.SendMessage(chatId, $"{scheduleText} Выберите пару: ", replyMarkup: markup, parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения расписания другой подгруппы для студента {StudentId}", studentInfo.Id);
                await _botClient.SendMessage(chatId, "Не удалось получить расписание другой подгруппы.");
            }
        }

        private async Task HandleNumberInputAsync(Message message, string userId, int number, IStudentRepository studentRepo, ILeaderService leaderService)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo != null) // Проверяем только регистрацию, не подгруппу
            {
                await _botClient.SendMessage(chatId, "Вы уже зарегистрированы.", replyMarkup: await CreateMainMenu(userId));
                return;
            }
            // Если студент не найден, ищем по номеру
            var studentExists = await studentRepo.GetByNumberInGroupAsync(number);
            if (studentExists == null)
            {
                await _botClient.SendMessage(chatId, "Такого номера нет.");
                return;
            }
            if (await studentRepo.IsNumberTakenAsync(number))
            {
                await _botClient.SendMessage(chatId, $"Номер {number} уже занят.");
                return;
            }
            try
            {
                // Сначала привязываем TelegramId
                studentExists.TelegramId = userId;
                await studentRepo.UpdateAsync(studentExists);
                await studentRepo.SaveChangesAsync();

                // Основной староста задан в конфиге телеграм айди, права выдаём сразу после его регистрации
                await leaderService.SyncMainLeadersAsync();

                _logger.LogInformation("Telegram ID {UserId} привязан к номеру {Number}", userId, number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка привязки Telegram ID {UserId} к номеру {Number}", userId, number);
                await _botClient.SendMessage(chatId, "Ошибка сохранения номера.");
                return;
            }
            // Регистрация завершена, отправляем главное меню
            await _botClient.SendMessage(chatId, $"Номер {number} сохранен! Регистрация завершена. 👍", replyMarkup: await CreateMainMenu(userId));
        }

        private async Task HandleOtherTextAsync(Message message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo != null) // Проверяем только регистрацию
            {
                await _botClient.SendMessage(chatId, "Используйте кнопки меню или введите команду.", replyMarkup: await CreateMainMenu(userId));
            }
            else // Если не зарегистрирован
            {
                await _botClient.SendMessage(chatId, "Привет! Используйте /start для регистрации.", replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private async Task ListCommandAsync(Message message, string userId, IStudentRepository studentRepo, IAttendanceRepository attendanceRepo, ILeaderService leaderService)
        {
            var chatId = message.Chat.Id;
            try
            {
                var isLeader = await leaderService.IsLeaderAsync(userId);

                // Проверка, является ли пользователь старостой
                if (!isLeader)
                {
                    await HandleOtherTextAsync(message, userId, studentRepo);
                    return;
                }
           
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
           
                // Получаем все отметки за сегодня
                var attendanceRecords = await attendanceRepo.GetTodayAttendanceAsync();

                var reportMessage = LeaderListFormatter.FormatGroupList(attendanceRecords, today);

                await _botClient.SendMessage(chatId, reportMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании списка отметившихся.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при формировании списка.", replyMarkup: await CreateMainMenu(userId));
            }
        }

        private async Task TimeCommandAsync(Message message, string userId, IStudentRepository  studentRepo)
        {
            var chatId = message.Chat.Id;

            try
            {
                // Проверка, является ли пользователь администратором
                if (!TelegramConfig.AdminTelegramIdsArray.Contains(userId))
                {
                    await HandleOtherTextAsync(message, userId, studentRepo);
                    return;
                }

                var utcNow = DateTime.UtcNow.TimeOfDay;
                var gmtNow = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(3)).TimeOfDay;

                var responseMessage = $"Время UTC: {utcNow.Hours:00}:{utcNow.Minutes:00}{Environment.NewLine}" +
                                      $"Локальное время (GMT+3): {gmtNow.Hours:00}:{gmtNow.Minutes:00}";

                await _botClient.SendMessage(chatId, responseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании ответа о текущем времени.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при вычислении времени.", replyMarkup: await CreateMainMenu(userId));
            }
        }


        private async Task SetSubgroupCommandAsync(Message message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo == null)
            {
                await _botClient.SendMessage(chatId, "Вы не зарегистрированы. Используйте /start.", replyMarkup: new ReplyKeyboardRemove());
                return;
            }

            // Проверяем, есть ли уже подгруппа
            if (studentInfo.Subgroup.HasValue)
            {
                await _botClient.SendMessage(chatId, $"Вы уже состоите в {(studentInfo.Subgroup.Value == Subgroup.First ? "1-й" : "2-й")} подгруппе. Используйте /remove_subgroup, чтобы покинуть её.", replyMarkup: await CreateMainMenu(userId));
                return;
            }

            // Предлагаем выбрать подгруппу
            var markup = new InlineKeyboardMarkup(new[]
            {
                 new InlineKeyboardButton("1-я подгруппа") { CallbackData = "setsubgroup_1" },
                 new InlineKeyboardButton("2-я подгруппа") { CallbackData = "setsubgroup_2" }
            });
            await _botClient.SendMessage(chatId, "Выберите подгруппу, к которой хотите присоединиться:", replyMarkup: markup);
        }

        private async Task RemoveSubgroupCommandAsync(Message message, string userId, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo == null)
            {
                await _botClient.SendMessage(chatId, "Вы не зарегистрированы. Используйте /start.", replyMarkup: new ReplyKeyboardRemove());
                return;
            }

            // Проверяем, есть ли подгруппа для удаления
            if (!studentInfo.Subgroup.HasValue)
            {
                await _botClient.SendMessage(chatId, "Вы не состоите ни в одной подгруппе.", replyMarkup: await CreateMainMenu(userId));
                return;
            }

            try
            {
                await studentRepo.UpdateStudentSubgroupAsync(userId, null); // Устанавливаем подгруппу в null
                await _botClient.SendMessage(chatId, "Вы покинули подгруппу. Теперь вы будете получать уведомления о парах обеих подгрупп.", replyMarkup: await CreateMainMenu(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления подгруппы для студента {UserId}", userId);
                await _botClient.SendMessage(chatId, "Ошибка при удалении подгруппы.");
            }
        }

        private async Task HandleSubgroupChoiceAsync(CallbackQuery callbackQuery, string userId, string callbackData, IStudentRepository studentRepo)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var message_id = callbackQuery.Message.MessageId;
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
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка кнопки", showAlert: true);
                return;
            }

            var studentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (studentInfo == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка: вы не зарегистрированы.", showAlert: true);
                return;
            }

            // Проверка: если у студента уже есть подгруппа, не меняем
            if (studentInfo.Subgroup.HasValue)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка: вы уже состоите в подгруппе.", showAlert: true);
                return;
            }

            try
            {
                await studentRepo.UpdateStudentSubgroupAsync(userId, selectedSubgroup);
                // Убираем клавиатуру с предыдущего сообщения
                await _botClient.EditMessageReplyMarkup(chatId, message_id, replyMarkup: null);
                // Отправляем новое сообщение с подтверждением
                await _botClient.SendMessage(chatId, $"Вы присоединились к {subgroupDisplay} подгруппе.", replyMarkup: await CreateMainMenu(userId));
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Подгруппа {subgroupDisplay} выбрана.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления подгруппы для студента {UserId}", userId);
                await _botClient.SendMessage(chatId, "Ошибка сохранения подгруппы.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка сохранения", showAlert: true);
            }
        }

        private async Task HandleAttendanceAsync(CallbackQuery callbackQuery, string userId, string callbackData, IStudentRepository studentRepo, IScheduleRepository scheduleRepo, IAttendanceRepository attendanceRepo)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var message_id = callbackQuery.Message.MessageId;

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
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка кнопки", showAlert: true);
                return;
            }

            var currentStudentDbId = (await studentRepo.GetByTelegramIdAsync(userId))?.Id;
            if (currentStudentDbId != studentDbId)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Это кнопка для другого студента.", showAlert: true);
                return;
            }

            if (!CanMarkAttendance())
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Время для отметки (до {_timeLimitHourUtc.Hours:00}:{_timeLimitHourUtc.Minutes:00} по UTC) уже вышло.", showAlert: true);
                try
                {
                    await _botClient.EditMessageReplyMarkup(chatId, message_id, replyMarkup: null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось убрать кнопку attend_ после лимита.");
                }
                return;
            }

            var scheduleItem = await scheduleRepo.GetByIdAsync(scheduleId);
            if (scheduleItem == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка получения предмета.", showAlert: true);
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
                    var newText = callbackQuery.Message.Text ?? string.Empty;
                    var clickButtonText = callbackQuery.Message.Text?.Split('\n').LastOrDefault();
                    if (!string.IsNullOrEmpty(clickButtonText))
                    {
                        newText = newText.Replace(clickButtonText, string.Empty);
                    }
                    await _botClient.EditMessageText(chatId, message_id, newText, replyMarkup: null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось убрать кнопку attend_, так как занятия сегодня нет.");
                }

                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Занятия нет в расписании на сегодня.", showAlert: true);
                return;
            }

            try
            {
                // Используем UTC время для отметки
                var utcNow = DateTime.UtcNow;
                // Используем репозиторий для отметки
                await attendanceRepo.MarkAttendanceAsync(studentDbId, scheduleId, utcNow);

                var originalMessageLines = callbackQuery.Message.Text.Split('\n');
                var header = originalMessageLines[0] + Environment.NewLine + originalMessageLines[1];
                var newText = $"{header}\n✅ Вы отмечены.";

                try
                {
                    await _botClient.EditMessageText(chatId, message_id, newText, replyMarkup: null);
                }
                catch (Exception ex) when (ex.Message.Contains("message is not modified"))
                {
                    // Ок, сообщение не изменилось
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка изменения сообщения attend_");
                    await _botClient.SendMessage(chatId, $"✅ Вы отмечены на занятии '{scheduleItem.Subject.Name}'.");
                }

                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Вы отмечены!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки посещаемости attend_ для студента {StudentId}, предмет {ScheduleId}", studentDbId, scheduleId);
                await _botClient.SendMessage(chatId, "Ошибка при отметке.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка записи", showAlert: true);
            }
        }

        private async Task HandleMarkOtherAttendanceAsync(CallbackQuery callbackQuery, string userId, string callbackData, IStudentRepository studentRepo, IScheduleRepository scheduleRepo, IAttendanceRepository attendanceRepo)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var message_id = callbackQuery.Message.MessageId;

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
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка кнопки", showAlert: true);
                return;
            }

            var currentStudentInfo = await studentRepo.GetByTelegramIdAsync(userId);
            if (currentStudentInfo == null || currentStudentInfo.Id != studentDbId)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Действие не разрешено.", showAlert: true);
                return;
            }

            if (!CanMarkAttendance())
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Время для отметки (до {_timeLimitHourUtc.Hours:00}:{_timeLimitHourUtc.Minutes:00} по UTC) уже вышло.", showAlert: true);
                try
                {
                    await _botClient.EditMessageReplyMarkup(chatId, message_id, replyMarkup: null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось убрать кнопки markother_ после лимита");
                }
                return;
            }

            var scheduleItem = await scheduleRepo.GetByIdAsync(scheduleId);
            if (scheduleItem == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка получения предмета.", showAlert: true);
                return;
            }

            try
            {
                // Используем UTC время для отметки
                var utcNow = DateTime.UtcNow;
                // Используем репозиторий для отметки
                await attendanceRepo.MarkAttendanceAsync(studentDbId, scheduleId, utcNow);

                var newText = callbackQuery.Message.Text + $"\n✅ Вы отмечены на занятии '{scheduleItem.Subject.Name}'.";

                try
                {
                    await _botClient.EditMessageText(chatId, message_id, newText, replyMarkup: null, parseMode: ParseMode.Markdown);
                }
                catch (Exception ex) when (ex.Message.Contains("message is not modified"))
                {
                    // Ок, сообщение не изменилось
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка изменения сообщения markother_");
                    await _botClient.SendMessage(chatId, $"✅ Вы отмечены на занятии '{scheduleItem.Subject.Name}'.");
                }

                await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Вы отмечены на '{scheduleItem.Subject.Name}'!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки посещаемости markother_ для студента {StudentId}, предмет {ScheduleId}", studentDbId, scheduleId);
                await _botClient.SendMessage(chatId, $"Ошибка при отметке на '{scheduleItem.Subject.Name}'.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка записи", showAlert: true);
            }
        }

        private bool CanMarkAttendance()
        {
            var nowUtc = DateTime.UtcNow;
            // Сравниваем время дня с _timeLimitHourUtc
            return nowUtc.TimeOfDay < _timeLimitHourUtc;
        }

        private bool IsWaitingForLeaderInput(string userId)
        {
            return WaitingLeaders.Contains(userId) || WaitingGroupListLeaders.Contains(userId);
        }

        private async Task ParseScheduleCommandAsync(Message message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            try
            {

                if (IsWaitingForLeaderInput(userId))
                {
                    await _botClient.SendMessage(chatId, "Бот уже ожидает данные от вас.", replyMarkup: new ReplyKeyboardRemove());
                    return;
                }

                var isLeader = await leaderService.IsLeaderAsync(userId);

                // Проверка, является ли пользователь старостой
                if (!isLeader)
                {
                    await HandleOtherTextAsync(message, userId, studentRepo);
                    return;
                }

                WaitingLeaders.Add(userId);
                await _botClient.SendMessage(chatId, "Готов принять новое расписание. Отправьте его от бота @knrtukaibot.", replyMarkup: new ReplyKeyboardRemove());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /parse_schedule.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при обработке команды.", replyMarkup: await CreateMainMenu(userId));
            }
            
        }

        private async Task ProcessScheduleTextAsync(Message message, string userId)
        {
            var chatId = message.Chat.Id;

            try
            {

                if (string.IsNullOrEmpty(message.Text))
                {
                    await _botClient.SendMessage(chatId, "Полученное сообщение не содержит текста расписания.");
                    return;
                }


                var scheduleData = ScheduleParser.ParseScheduleText(message.Text, _logger);
                if (scheduleData == null || !scheduleData.Success)
                {
                    await _botClient.SendMessage(chatId, "Ошибка при разборе формата расписания. Проверьте формат и повторите.");
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
                await _botClient.SendMessage(chatId, "Расписание успешно обновлено.", replyMarkup: await CreateMainMenu(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге или сохранении расписания.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при обработке расписания.", replyMarkup: await CreateMainMenu(userId));
            }
        }

        private async Task ParseGroupListCommandAsync(Message message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            try
            {
                if (IsWaitingForLeaderInput(userId))
                {
                    await _botClient.SendMessage(chatId, "Бот уже ожидает данные от вас.", replyMarkup: new ReplyKeyboardRemove());
                    return;
                }

                var isLeader = await leaderService.IsLeaderAsync(userId);
                if (!isLeader)
                {
                    await HandleOtherTextAsync(message, userId, studentRepo);
                    return;
                }

                WaitingGroupListLeaders.Add(userId);
                await _botClient.SendMessage(chatId,
                    $"Готов принять новый список группы.{Environment.NewLine}{Environment.NewLine}{GroupListFormatExample}",
                    replyMarkup: new ReplyKeyboardRemove());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /parse_group_list.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при обработке команды.", replyMarkup: await CreateMainMenu(userId));
            }
        }

        private async Task ProcessGroupListTextAsync(Message message, string userId)
        {
            var chatId = message.Chat.Id;

            try
            {
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    await _botClient.SendMessage(chatId,
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

                    await _botClient.SendMessage(chatId,
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

                if (!await leaderService.IsLeaderAsync(userId))
                {
                    await _botClient.SendMessage(chatId, "Команда доступна только старосте.", replyMarkup: await CreateMainMenu(userId));
                    return;
                }

                var newStudents = parsedGroupList.Data;

                // При первом развёртывании студентов в базе ещё нет, поэтому староста из конфига списка
                // группы может не быть зарегистрированным студентом — тогда искать его в новом списке нечем
                var currentLeaderStudent = await studentRepo.GetByTelegramIdAsync(userId);
                Student? currentLeaderInNewList = null;

                if (currentLeaderStudent != null)
                {
                    // Староста уже зарегистрирован: не даём ему случайно вычеркнуть себя из группы
                    currentLeaderInNewList = FindMatchingStudent(newStudents, currentLeaderStudent);
                    if (currentLeaderInNewList == null)
                    {
                        await _botClient.SendMessage(chatId,
                            $"Не нашёл вас в новом списке группы по ФИО или номеру. Список не обновлён.{Environment.NewLine}{Environment.NewLine}{GroupListFormatExample}",
                            replyMarkup: await CreateMainMenu(userId));
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

                // Основные старосты заданы телеграм айди в конфиге, после смены списка права им нужно вернуть
                await leaderService.SyncMainLeadersAsync();

                await transaction.CommitAsync();

                var preservedRegistrationsCount = newStudents.Count(student => !string.IsNullOrWhiteSpace(student.TelegramId));

                // Староста из конфига после первой загрузки списка ещё не привязан к студенту
                var registrationHint = currentLeaderStudent == null
                    ? $"{Environment.NewLine}Вы ещё не зарегистрированы в боте — отправьте /start и выберите свой номер в списке."
                    : string.Empty;

                await _botClient.SendMessage(chatId,
                    $"Список группы успешно обновлён. Загружено студентов: {newStudents.Count}. " +
                    $"Сохранено регистраций Telegram: {preservedRegistrationsCount}.{registrationHint}",
                    replyMarkup: await CreateMainMenu(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге или сохранении списка группы.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при обработке списка группы.", replyMarkup: await CreateMainMenu(userId));
            }
        }


        /// <summary>
        /// Проверяет, что группа делится на подгруппы. Если деления нет, команды подгрупп вызывать нельзя
        /// </summary>
        private async Task<bool> EnsureSubgroupsEnabledAsync(Message message, string userId, IGroupSettingsRepository groupSettingsRepo)
        {
            if (await groupSettingsRepo.HasSubgroupsAsync())
                return true;

            await _botClient.SendMessage(message.Chat.Id,
                "Группа не делится на подгруппы, поэтому команда недоступна. Изменить это может староста командой /subgroups.",
                replyMarkup: await CreateMainMenu(userId));

            return false;
        }

        private async Task SubgroupsSettingCommandAsync(Message message, string userId, ILeaderService leaderService,
            IGroupSettingsRepository groupSettingsRepo, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            try
            {
                if (!await leaderService.IsLeaderAsync(userId))
                {
                    await HandleOtherTextAsync(message, userId, studentRepo);
                    return;
                }

                var hasSubgroups = await groupSettingsRepo.HasSubgroupsAsync();
                var markup = new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton("Делится на подгруппы") { CallbackData = $"{SubgroupsSettingCallbackPrefix}1" },
                    new InlineKeyboardButton("Не делится") { CallbackData = $"{SubgroupsSettingCallbackPrefix}0" }
                });

                await _botClient.SendMessage(chatId,
                    $"Сейчас группа {(hasSubgroups ? "делится на подгруппы" : "не делится на подгруппы")}." +
                    $"{Environment.NewLine}Выберите, как должно быть:",
                    replyMarkup: markup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /subgroups.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при обработке команды.", replyMarkup: await CreateMainMenu(userId));
            }
        }

        private async Task HandleSubgroupsSettingChoiceAsync(CallbackQuery callbackQuery, string userId, string callbackData,
            ILeaderService leaderService, IGroupSettingsRepository groupSettingsRepo, IStudentRepository studentRepo)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;

            if (!await leaderService.IsLeaderAsync(userId))
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Команда доступна только старосте.", showAlert: true);
                return;
            }

            if (callbackData != $"{SubgroupsSettingCallbackPrefix}1" && callbackData != $"{SubgroupsSettingCallbackPrefix}0")
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка кнопки", showAlert: true);
                return;
            }

            var hasSubgroups = callbackData == $"{SubgroupsSettingCallbackPrefix}1";

            try
            {
                await groupSettingsRepo.SetHasSubgroupsAsync(hasSubgroups);

                // Без деления группы подгруппы студентов теряют смысл, поэтому очищаем их
                if (!hasSubgroups)
                    await studentRepo.ResetSubgroupsAsync();

                await _botClient.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null);
                await _botClient.SendMessage(chatId,
                    hasSubgroups
                        ? "Группа делится на подгруппы. Студентам доступны /set_subgroup, /remove_subgroup и /attend_other_group."
                        : "Группа не делится на подгруппы. Команды работы с подгруппами скрыты, подгруппы студентов сброшены.",
                    replyMarkup: await CreateMainMenu(userId));
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Настройка сохранена.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения настройки деления группы на подгруппы.");
                await _botClient.SendMessage(chatId, "Ошибка сохранения настройки.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка сохранения", showAlert: true);
            }
        }

        private async Task AddLeaderCommandAsync(Message message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            try
            {
                if (!await leaderService.IsLeaderAsync(userId))
                {
                    await HandleOtherTextAsync(message, userId, studentRepo);
                    return;
                }

                var candidates = await leaderService.GetAssistantCandidatesAsync();
                if (candidates.Count == 0)
                {
                    await _botClient.SendMessage(chatId,
                        "Назначать некого: все зарегистрированные в боте студенты уже имеют права старосты.",
                        replyMarkup: await CreateMainMenu(userId));
                    return;
                }

                var leaders = await leaderService.GetLeadersAsync();
                await _botClient.SendMessage(chatId,
                    $"{FormatLeaders(leaders)}{Environment.NewLine}Выберите, кого назначить помощником старосты:",
                    replyMarkup: CreateStudentsMarkup(candidates, AddLeaderCallbackPrefix));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /add_leader.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при обработке команды.", replyMarkup: await CreateMainMenu(userId));
            }
        }

        private async Task RemoveLeaderCommandAsync(Message message, string userId, ILeaderService leaderService, IStudentRepository studentRepo)
        {
            var chatId = message.Chat.Id;
            try
            {
                if (!await leaderService.IsLeaderAsync(userId))
                {
                    await HandleOtherTextAsync(message, userId, studentRepo);
                    return;
                }

                var assistants = await leaderService.GetRemovableAssistantsAsync();
                if (assistants.Count == 0)
                {
                    await _botClient.SendMessage(chatId,
                        "Снимать некого: помощников старосты сейчас нет. Основного старосту можно сменить только через конфиг.",
                        replyMarkup: await CreateMainMenu(userId));
                    return;
                }

                await _botClient.SendMessage(chatId, "Выберите, с кого снять права помощника старосты:",
                    replyMarkup: CreateStudentsMarkup(assistants, RemoveLeaderCallbackPrefix));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке команды /remove_leader.");
                await _botClient.SendMessage(chatId, "Произошла ошибка при обработке команды.", replyMarkup: await CreateMainMenu(userId));
            }
        }

        private Task HandleAddLeaderChoiceAsync(CallbackQuery callbackQuery, string userId, string callbackData, ILeaderService leaderService)
        {
            return HandleLeaderChangeAsync(callbackQuery, userId, callbackData, AddLeaderCallbackPrefix, leaderService,
                leaderService.AddAssistantAsync,
                student => $"{student.GetFullName()} назначен помощником старосты.",
                "Вам выданы права помощника старосты. Список доступных команд — /help.");
        }

        private Task HandleRemoveLeaderChoiceAsync(CallbackQuery callbackQuery, string userId, string callbackData, ILeaderService leaderService)
        {
            return HandleLeaderChangeAsync(callbackQuery, userId, callbackData, RemoveLeaderCallbackPrefix, leaderService,
                leaderService.RemoveAssistantAsync,
                student => $"С {student.GetFullName()} сняты права помощника старосты.",
                "С вас сняты права помощника старосты.");
        }

        /// <summary>
        /// Общая обработка кнопок назначения и снятия помощника старосты
        /// </summary>
        private async Task HandleLeaderChangeAsync(CallbackQuery callbackQuery, string userId, string callbackData, string callbackPrefix,
            ILeaderService leaderService, Func<int, Task<IDataResult<Student>>> changeLeaderAsync,
            Func<Student, string> formatResultForLeader, string textForStudent)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;

            if (!await leaderService.IsLeaderAsync(userId))
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Команда доступна только старосте.", showAlert: true);
                return;
            }

            if (!int.TryParse(callbackData[callbackPrefix.Length..], out var studentId))
            {
                _logger.LogError("Ошибка парсинга callback {CallbackPrefix}: {CallbackData}", callbackPrefix, callbackData);
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка кнопки", showAlert: true);
                return;
            }

            try
            {
                var changeResult = await changeLeaderAsync(studentId);

                // Кнопки одноразовые: список старост уже изменился
                await _botClient.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null);

                if (!changeResult.Success)
                {
                    var errorText = changeResult.Errors[0].Message;
                    await _botClient.SendMessage(chatId, errorText, replyMarkup: await CreateMainMenu(userId));
                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, errorText, showAlert: true);
                    return;
                }

                await _botClient.SendMessage(chatId, formatResultForLeader(changeResult.Data), replyMarkup: await CreateMainMenu(userId));
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Готово.");

                await NotifyStudentAboutLeaderRightsAsync(changeResult.Data, textForStudent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка изменения списка старост. CallbackData={CallbackData}", callbackData);
                await _botClient.SendMessage(chatId, "Произошла ошибка при изменении списка старост.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка сохранения", showAlert: true);
            }
        }

        /// <summary>
        /// Сообщает студенту об изменении прав, заодно обновляя ему меню
        /// </summary>
        private async Task NotifyStudentAboutLeaderRightsAsync(Student student, string text)
        {
            if (string.IsNullOrWhiteSpace(student.TelegramId))
                return;

            try
            {
                await _botClient.SendMessage(student.TelegramId, text, replyMarkup: await CreateMainMenu(student.TelegramId));
            }
            catch (Exception ex)
            {
                // Студент мог заблокировать бота, для смены прав это не критично
                _logger.LogWarning(ex, "Не удалось уведомить студента {StudentId} об изменении прав старосты.", student.Id);
            }
        }

        private static string FormatLeaders(IReadOnlyCollection<Student> leaders)
        {
            if (leaders.Count == 0)
                return $"Сейчас права старосты не выданы никому.{Environment.NewLine}";

            var leaderLines = leaders.Select(leader => $"• {leader.GetFullName()}");

            return $"Сейчас права старосты есть у:{Environment.NewLine}{string.Join(Environment.NewLine, leaderLines)}{Environment.NewLine}";
        }

        private static InlineKeyboardMarkup CreateStudentsMarkup(IEnumerable<Student> students, string callbackPrefix)
        {
            var markup = new InlineKeyboardMarkup();
            foreach (var student in students)
            {
                var button = new InlineKeyboardButton($"{student.NumberInGroup}. {student.GetFullName()}")
                {
                    CallbackData = $"{callbackPrefix}{student.Id}"
                };
                markup.InlineKeyboard = markup.InlineKeyboard.Append(new[] { button }).ToArray();
            }

            return markup;
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

        private static void PreserveStudentRegistrations(IReadOnlyCollection<Student> newStudents, IEnumerable<Student> archivedStudents)
        {
            var registeredStudentsByFullName = archivedStudents
                .Where(student => !string.IsNullOrWhiteSpace(student.TelegramId))
                .GroupBy(NormalizeFullName)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single());

            foreach (var newStudent in newStudents)
            {
                if (!registeredStudentsByFullName.TryGetValue(NormalizeFullName(newStudent), out var archivedStudent))
                    continue;

                newStudent.TelegramId = archivedStudent.TelegramId;
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

        private async Task<ReplyKeyboardMarkup> CreateMainMenu(string? telegramId = null)
        {
            List<KeyboardButton> commandButtons = [];
            using var scope = _scopeFactory.CreateScope();
            var groupSettingsRepo = scope.ServiceProvider.GetRequiredService<IGroupSettingsRepository>();
            var hasSubgroups = await groupSettingsRepo.HasSubgroupsAsync();

            if(!string.IsNullOrEmpty(telegramId))
            {
                var leaderService = scope.ServiceProvider.GetRequiredService<ILeaderService>();
                var isLeader = await leaderService.IsLeaderAsync(telegramId);
                if (isLeader)
                {
                    commandButtons.Add(new("/list"));
                    commandButtons.Add(new("/parse_schedule"));
                    commandButtons.Add(new("/parse_group_list"));
                    commandButtons.Add(new("/subgroups"));
                    commandButtons.Add(new("/add_leader"));
                    commandButtons.Add(new("/remove_leader"));
                }

                var isAdmin = TelegramConfig.AdminTelegramIdsArray.Contains(telegramId);
                if (isAdmin)
                {
                    commandButtons.Add(new("/time"));
                }
            }

            var menuRows = new List<KeyboardButton[]>();
            menuRows.Add([new("/schedule"), new("/info")]);

            // Кнопку отметки за другую подгруппу показываем, только когда группа делится на подгруппы
            if (hasSubgroups)
                menuRows.Add([new("/attend_other_group"), new("/help")]);
            else
                menuRows.Add([new("/help")]);

            // Команд старосты много, раскладываем их по две в ряд, чтобы клавиатура не расползалась
            menuRows.AddRange(commandButtons.Chunk(2));

            var menu = new ReplyKeyboardMarkup(menuRows)
            {
                ResizeKeyboard = true
            };
            return menu;
        }

        /// <summary>
        /// Метод для отправки уведомлений
        /// </summary>
        private async Task SendScheduledNotificationsAsync()
        {
            _logger.LogInformation("Запуск задачи по отправке уведомлений о начале пар.");
            using var scope = _scopeFactory.CreateScope();
            var logNotificatorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
            try
            {   
                var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                // Получаем неотправленные уведомления
                var unsentNotifications = await notificationRepo.GetUnsentStartClassNotificationsAsync();

                foreach (var notification in unsentNotifications)
                {
                    try
                    {
                        var student = notification.Student;
                        if (student == null || string.IsNullOrEmpty(student.TelegramId))
                        {
                            await logNotificatorService.LogAndNotifyAdminsAsync($"Уведомление для студента {notification.StudentId} не может быть отправлено: студент не найден или не имеет Telegram ID.");
                            // Помечаем как отправленное, так как невозможно отправить
                            await notificationRepo.MarkAsSentAsync(notification);
                            continue;
                        }

                        //Для типа NotificationType.EndDayReport в поле Text хранится просто строка с сообщением, можно ничего не сериализовать
                        string notificationText = notification.Text;

                        //Для типа NotificationType.StartClass надо добавить десериализацию из JSON
                        InlineKeyboardMarkup? inlineKeyboard = null;

                        if (notification.NotificationType == NotificationType.StartClass)
                        {
                            // Десериализуем JSON из поля Text
                            using JsonDocument doc = JsonDocument.Parse(notificationText);
                            var root = doc.RootElement;

                            notificationText = root.GetProperty("Text").GetString() ?? string.Empty;

                            if (root.TryGetProperty("InlineKeyboard", out JsonElement keyboardElement))
                            {
                                // Десериализуем InlineKeyboardMarkup из JSON
                                var keyboardJsonString = keyboardElement.GetRawText();
                                inlineKeyboard = JsonSerializer.Deserialize<InlineKeyboardMarkup>(keyboardJsonString);
                            }
                        }
                       
                        // Отправляем сообщение
                        await _botClient.SendMessage(
                            chatId: student.TelegramId, // Используем TelegramId студента
                            text: notificationText,
                            replyMarkup: inlineKeyboard // Передаем клавиатуру, если она была
                        );

                        // Помечаем уведомление как отправленное в базе данных
                        await notificationRepo.MarkAsSentAsync(notification);
                        _logger.LogDebug("Уведомление для студента {StudentId} (TelegramId: {TelegramId}) успешно отправлено.", student.Id, student.TelegramId);

                    }
                    catch (ApiRequestException ex) when (ex.Message.Contains("bot was blocked by the user") || ex.ErrorCode == 403)
                    {
                        // Обработка случая, когда бот заблокирован пользователем
                        _logger.LogWarning(ex, "Ошибка при отправке уведомления из базы для студента {StudentId}, {NotificationId}. Бот заблокирован пользователем.", notification.StudentId, notification.Id);
                        await notificationRepo.WriteSendingErrorAsync(notification, ex.Message);
                    }
                    catch (ApiRequestException ex) when (ex.Message.Contains("chat not found"))
                    {
                        _logger.LogWarning(ex, "Ошибка при отправке уведомления из базы для студента {StudentId}, {NotificationId}. Чат не найден.", notification.StudentId, notification.Id);
                        await notificationRepo.WriteSendingErrorAsync(notification, ex.Message);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) // Не ловим CancellationToken
                    {
                        _logger.LogError(ex, "Ошибка при отправке уведомления из базы для студента {StudentId}, {NotificationId}.", notification.StudentId, notification.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                await logNotificatorService.LogAndNotifyAdminsAsync($"Неожиданная ошибка в цикле отправки уведомлений. Текст ошибки: {ex.Message}", ex);
            }
            finally
            {
                _logger.LogInformation("Задача по отправке уведомлений о начале пар завершена.");
            }
        }

    }
}
