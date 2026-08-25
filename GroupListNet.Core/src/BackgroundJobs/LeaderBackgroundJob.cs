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
    public class LeaderBackgroundJob : BackgroundService
    {
        private readonly ILogger<LeaderBackgroundJob> _logger;
        private readonly TimeSpan deadlineTime;
        private readonly IServiceScopeFactory _scopeFactory;
        public LeaderBackgroundJob(ILogger<LeaderBackgroundJob> logger, 
            IConfiguration config,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            var deadLineDefaultValue = 17;
            _scopeFactory = scopeFactory;
            try
            {
                var deadLineTimeStr = config.GetSection(SendingSettings.SendingSettingsSectionInConfig).Get<SendingSettings>()?.DeadlineTime;
                if (!TimeSpan.TryParse(deadLineTimeStr, CultureInfo.InvariantCulture, out deadlineTime))
                {
                    deadlineTime = new TimeSpan(deadLineDefaultValue, 0, 0);
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var logNotificatiorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
                        logNotificatiorService.LogAndNotifyAdminsAsync($"Ошибка в {nameof(LeaderBackgroundJob)}, " +
                        $"не удалось спарсить DeadlineTime из конфига. Используемое значение = {deadLineDefaultValue}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} error getting value from config! Using default value = {DefaultValue}{NewLine}",
                    nameof(LeaderBackgroundJob), deadLineDefaultValue, Environment.NewLine);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckScheduleAndNotifyAsync();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); //Ожидание 1 минута
            }
        }

        public async Task CheckScheduleAndNotifyAsync()
        
        {
            try
            {
                var timeLimitPassed = DateTime.UtcNow.TimeOfDay >= deadlineTime;

                if (!timeLimitPassed)
                {
                    return;
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var _logNotificatorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
                    var _leaderRepository = scope.ServiceProvider.GetRequiredService<ILeaderRepository>();
                    var _notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                    var leaderIds = await _leaderRepository.GetLeaderIds();
                    if (leaderIds.Length == 0)
                    {
                        await _logNotificatorService.NotifyAdminsAsync("Не удалось найти старосту в таблице старост. Ежедневный отчёт не был отправлен.");
                        return;
                    }

                    var _studentRepository = scope.ServiceProvider.GetRequiredService<IStudentRepository>();

                    // Текст отчёта собирается при отправке, здесь только решаем, кому он нужен
                    // Отчёт нужен в каждом мессенджере, который староста привязал: он может читать бота где угодно
                    foreach (var messenger in Enum.GetValues<MessengerType>())
                    {
                        var leaderIdsForMessenger = await _notificationRepository.ExceptSentReportAsync(leaderIds, today, messenger);

                        foreach (var leaderId in leaderIdsForMessenger)
                        {
                            try
                            {
                                var leader = await _studentRepository.GetByIdAsync(leaderId);

                                // Старосте без этого мессенджера отчёт слать некуда
                                if (leader == null || string.IsNullOrWhiteSpace(leader.GetMessengerId(messenger)))
                                    continue;

                                await _notificationRepository.RecordDailyReportSentAsync(leaderId, today, messenger);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "В {ClassName} ошибка создания отчёта старосте. Id старосты:{LeaderId}. День:{Today}. " +
                                    "Мессенджер:{Messenger}.{NewLine}",
                             nameof(LeaderBackgroundJob), leaderId, today, messenger, Environment.NewLine);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "В {ClassName} неизвестная ошибка.{NewLine}",
                    nameof(LeaderBackgroundJob), Environment.NewLine);
            }
        }
    }
}
