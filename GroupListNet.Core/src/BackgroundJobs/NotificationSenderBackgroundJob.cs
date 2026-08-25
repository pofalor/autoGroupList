using GroupListNet.Core.src.Bot;
using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using GroupListNet.Core.src.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GroupListNet.Core.src.BackgroundJobs
{
    /// <summary>
    /// Отправляет накопленные уведомления во все настроенные мессенджеры.
    /// Каждое уведомление привязано к своему мессенджеру, поэтому дублей не будет
    /// </summary>
    public class NotificationSenderBackgroundJob : BackgroundService
    {
        private readonly ILogger<NotificationSenderBackgroundJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEnumerable<IMessengerClient> _clients;
        private readonly TimeSpan _notificationCheckInterval = TimeSpan.FromMinutes(1);

        public NotificationSenderBackgroundJob(ILogger<NotificationSenderBackgroundJob> logger,
            IServiceScopeFactory scopeFactory,
            IEnumerable<IMessengerClient> clients)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _clients = clients;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendScheduledNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "В {ClassName} неизвестная ошибка в цикле отправки уведомлений.{NewLine}",
                        nameof(NotificationSenderBackgroundJob), Environment.NewLine);
                }
                finally
                {
                    await Task.Delay(_notificationCheckInterval, stoppingToken); // Ждем установленный интервал перед следующей проверкой
                }
            }
        }

        private async Task SendScheduledNotificationsAsync()
        {
            _logger.LogInformation("Запуск задачи по отправке уведомлений о начале пар.");
            using var scope = _scopeFactory.CreateScope();
            var logNotificatorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
            try
            {
                var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                foreach (var client in _clients.Where(candidate => candidate.IsEnabled))
                {
                    // Получаем неотправленные уведомления этого мессенджера
                    var unsentNotifications = await notificationRepo.GetUnsentNotificationsAsync(client.Messenger);

                    foreach (var notification in unsentNotifications)
                    {
                        await SendOneAsync(client, notification, notificationRepo, logNotificatorService);
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

        private async Task SendOneAsync(IMessengerClient client,
            Notification notification,
            INotificationRepository notificationRepo,
            ILogNotificatorService logNotificatorService)
        {
            try
            {
                var student = notification.Student;
                var messengerId = student?.GetMessengerId(client.Messenger);
                if (student == null || string.IsNullOrEmpty(messengerId))
                {
                    await logNotificatorService.LogAndNotifyAdminsAsync($"Уведомление для студента {notification.StudentId} " +
                        $"не может быть отправлено: студент не найден или не привязал {client.Messenger}.");
                    // Помечаем как отправленное, так как невозможно отправить
                    await notificationRepo.MarkAsSentAsync(notification);
                    return;
                }

                // Для типа NotificationType.EndDayReport в поле Text хранится просто строка с сообщением
                var notificationText = notification.Text;
                BotKeyboard? keyboard = null;

                // Для типа NotificationType.StartClass текст и кнопки лежат в JSON
                if (notification.NotificationType == NotificationType.StartClass)
                {
                    (notificationText, keyboard) = NotificationPayload.Parse(notification.Text);
                }

                await client.SendMessageAsync(messengerId, notificationText, keyboard);

                // Помечаем уведомление как отправленное в базе данных
                await notificationRepo.MarkAsSentAsync(notification);
                _logger.LogDebug("Уведомление для студента {StudentId} ({Messenger}: {MessengerId}) успешно отправлено.",
                    student.Id, client.Messenger, messengerId);
            }
            catch (MessengerSendException ex) when (ex.IsPermanent)
            {
                // Студент заблокировал бота или запретил сообщения — повторять отправку бессмысленно
                _logger.LogWarning(ex, "Ошибка при отправке уведомления из базы для студента {StudentId}, {NotificationId} в {Messenger}. " +
                    "Отправка невозможна.", notification.StudentId, notification.Id, client.Messenger);
                await notificationRepo.WriteSendingErrorAsync(notification, ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) // Не ловим CancellationToken
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления из базы для студента {StudentId}, {NotificationId} в {Messenger}.",
                    notification.StudentId, notification.Id, client.Messenger);
            }
        }
    }
}
