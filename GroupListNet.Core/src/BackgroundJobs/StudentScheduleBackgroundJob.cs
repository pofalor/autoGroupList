using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Enums;
using GroupListNet.Core.src.Services;
using GroupListNet.Core.src.Services.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace GroupListNet.Core.src.BackgroundJobs
{
    public class StudentScheduleBackgroundJob : BackgroundService
    {
        private readonly ILogger<StudentScheduleBackgroundJob> _logger;
        private readonly TimeSpan _timeLimitHourUtc;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _notificationCheckInterval = TimeSpan.FromMinutes(1); // Интервал проверки, раз в минуту
        public StudentScheduleBackgroundJob(ILogger<StudentScheduleBackgroundJob> logger, 
            IConfiguration config,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            var timeLimitHourUtcDefault = 17;
            _scopeFactory = scopeFactory;
            try
            {
                var deadLineTimeStr = config.GetSection(SendingSettings.SendingSettingsSectionInConfig).Get<SendingSettings>()?.DeadlineTime;
                if (!TimeSpan.TryParse(deadLineTimeStr, CultureInfo.InvariantCulture, out this._timeLimitHourUtc))
                {
                    this._timeLimitHourUtc = new TimeSpan(timeLimitHourUtcDefault, 0, 0);
                    using var scope = _scopeFactory.CreateScope();
                    var logNotificatiorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
                    logNotificatiorService.LogAndNotifyAdminsAsync($"Ошибка в {nameof(StudentScheduleBackgroundJob)}, " +
                    $"не удалось спарсить DeadlineTime из конфига. Используемое значение = {_timeLimitHourUtc}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} error getting value from config! Using default value = {DefaultValue}{NewLine}",
                    nameof(StudentScheduleBackgroundJob), timeLimitHourUtcDefault, Environment.NewLine);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{ClassName} started.", nameof(StudentScheduleBackgroundJob));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckScheduleAndNotifyAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "В {ClassName} неизвестная ошибка в цикле проверки.{NewLine}",
                        nameof(StudentScheduleBackgroundJob), Environment.NewLine);
                }
                finally
                {
                    await Task.Delay(_notificationCheckInterval, stoppingToken); // Ждем установленный интервал перед следующей проверкой
                }
            }

            _logger.LogInformation("{ClassName} stopped.", nameof(StudentScheduleBackgroundJob));
        }

        public async Task CheckScheduleAndNotifyAsync()
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                // Проверяем, прошло ли установленное время дедлайна сегодня
                var timeLimitPassed = utcNow.TimeOfDay >= _timeLimitHourUtc;


                if (timeLimitPassed)
                {
                    return;
                }

                var today = DateOnly.FromDateTime(utcNow);
                var weekNumber = ISOWeek.GetWeekOfYear(utcNow);
                var weekType = (weekNumber % 2 == 0) ? WeekType.Even : WeekType.Odd;
                var dayOfWeek = utcNow.DayOfWeek;

                using var scope = _scopeFactory.CreateScope();
               
                //Получаем все предметы, по которым пора слать уведомления
                var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                
                // дата и время в GMT+3 
                var gmtPlus3 = new DateTimeOffset(utcNow, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(3)).TimeOfDay;

                var scheduleForNotify = await scheduleRepo.GetStartedClassesAsync(weekType, dayOfWeek, gmtPlus3);

                if (!scheduleForNotify.Any())
                {
                    return;
                }

                var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
                var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                // Получаем всех студентов, привязавших хотя бы один мессенджер
                var registeredStudents = await studentRepo.GetStudentsWithAnyMessengerAsync();

                foreach (var student in registeredStudents)
                {
                    // Определяем подгруппу студента для получения расписания
                    // Если у студента нет подгруппы (null), он получает уведомления о парах обеих подгрупп
                    var studentSubgroup = student.Subgroup;

                    foreach (var scheduleItem in scheduleForNotify)
                    {
                        try
                        {
                            if (studentSubgroup.HasValue && scheduleItem.Subgroup.HasValue && scheduleItem.Subgroup != studentSubgroup)
                                continue;

                            // Текст и кнопка собираются при отправке из расписания,
                            // в базе лежит только сам факт уведомления
                            // Студент мог привязать оба мессенджера — уведомление создаём в каждый из них
                            foreach (var messenger in student.GetLinkedMessengers())
                            {
                                // Уведомление отправляется, если время начала пары наступило или прошло, но не позже дедлайна (если дедлайн еще не прошёл)
                                // Или отправляется, даже если дедлайн прошёл, но кнопка не нужна (только уведомление о том, что пара началась)
                                // Важно: проверить, не было ли уже отправлено уведомление
                                var notificationAlreadySent = await notificationRepo.IsStartClassNotificationSentAsync(student.Id, scheduleItem.Id, today, messenger);

                                if (!notificationAlreadySent)
                                {
                                    // Сохраняем уведомление в базу данных
                                    await notificationRepo.RecordStartClassNotificationAsync(student.Id, scheduleItem.Id, messenger);
                                    _logger.LogDebug("Уведомление для студента {StudentId} по предмету {ScheduleId} в {Messenger} сохранено в базу.",
                                        student.Id, scheduleItem.Id, messenger);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Ошибка сохранения уведомления в базу для студента {StudentId}, предмета {ScheduleId}.", student.Id, scheduleItem.Id);
                            // Логируем ошибку, но не прерываем обработку других студентов/предметов
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "В {ClassName} неизвестная ошибка.{NewLine}",
                    nameof(StudentScheduleBackgroundJob), Environment.NewLine);
            }
        }
    }
}
