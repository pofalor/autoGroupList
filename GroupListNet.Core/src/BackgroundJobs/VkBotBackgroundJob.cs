using GroupListNet.Core.src.Bot;
using GroupListNet.Core.src.Bot.Clients;
using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VkNet.Enums.StringEnums;
using VkNet.Exception;
using VkNet.Model;

namespace GroupListNet.Core.src.BackgroundJobs
{
    /// <summary>
    /// Приём сообщений из ВКонтакте через Bots Long Poll.
    /// Вся логика команд общая и живёт в <see cref="BotUpdateHandler"/>
    /// </summary>
    public class VkBotBackgroundJob : BackgroundService
    {
        /// <summary>
        /// Сколько секунд держать запрос к серверу Long Poll, пока не появятся события
        /// </summary>
        private const int LongPollWaitSeconds = 25;

        private readonly ILogger<VkBotBackgroundJob> _logger;
        private readonly VkMessengerClient _client;
        private readonly BotUpdateHandler _handler;
        private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(10);

        public VkBotBackgroundJob(ILogger<VkBotBackgroundJob> logger,
            VkMessengerClient client,
            BotUpdateHandler handler)
        {
            _logger = logger;
            _client = client;
            _handler = handler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_client.IsEnabled)
            {
                _logger.LogWarning("Бот ВКонтакте не настроен, {ClassName} не запущен.", nameof(VkBotBackgroundJob));
                return;
            }

            var groupId = _client.GroupId;
            if (groupId == null)
            {
                // Без айди сообщества сервер Long Poll не получить, а бесконечно пробовать бессмысленно
                _logger.LogError("Не задан или задан не числом {SettingName}, {ClassName} не запущен.",
                    VkMessengerClient.GroupIdSettingName, nameof(VkBotBackgroundJob));
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ReceiveUpdatesAsync(groupId.Value, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (LongPollException ex)
                {
                    // Ключ и метка времени Long Poll живут недолго, это ожидаемо: просто берём их заново
                    _logger.LogInformation(ex, "Соединение Long Poll ВКонтакте устарело, переподключаемся.");
                }
                catch (Exception ex)
                {
                    // Ключ Long Poll протухает, а сервер может отвечать ошибкой — переподключаемся с начала
                    _logger.LogError(ex, "Ошибка получения обновлений ВКонтакте, переподключение через {Delay}.", _reconnectDelay);
                    await Task.Delay(_reconnectDelay, stoppingToken);
                }
            }
        }

        private async Task ReceiveUpdatesAsync(ulong groupId, CancellationToken stoppingToken)
        {
            var server = await _client.Api.Groups.GetLongPollServerAsync(groupId, stoppingToken);
            var ts = server.Ts;

            _logger.LogInformation("Бот ВКонтакте подключён к Long Poll сообщества {GroupId}.", groupId);

            while (!stoppingToken.IsCancellationRequested)
            {
                var history = await _client.Api.Groups.GetBotsLongPollHistoryAsync(new BotsLongPollHistoryParams
                {
                    Server = server.Server,
                    Key = server.Key,
                    Ts = ts,
                    Wait = LongPollWaitSeconds
                }, stoppingToken);

                ts = history.Ts;

                if (history.Updates == null)
                    continue;

                foreach (var update in history.Updates)
                {
                    await HandleUpdateAsync(update);
                }
            }
        }

        private async Task HandleUpdateAsync(GroupUpdate update)
        {
            try
            {
                switch (update.Type?.Value)
                {
                    case GroupUpdateType.MessageNew when update.Instance is MessageNew messageNew:
                        var message = ToBotMessage(messageNew);
                        if (message != null)
                            await _handler.HandleMessageAsync(_client, message);
                        break;

                    case GroupUpdateType.MessageEvent when update.Instance is MessageEvent messageEvent:
                        var callback = ToBotCallback(messageEvent);
                        if (callback != null)
                            await _handler.HandleCallbackAsync(_client, callback);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке обновления ВКонтакте типа {UpdateType}.", update.Type?.Value);
            }
        }

        private static BotMessage? ToBotMessage(MessageNew messageNew)
        {
            var message = messageNew.Message;
            if (message?.Text == null || message.FromId == null || message.PeerId == null)
                return null;

            return new BotMessage
            {
                Messenger = MessengerType.Vk,
                UserId = message.FromId.Value.ToString(),
                ChatId = message.PeerId.Value.ToString(),
                Text = message.Text
            };
        }

        private static BotCallback? ToBotCallback(MessageEvent messageEvent)
        {
            if (messageEvent.UserId == null || messageEvent.PeerId == null || messageEvent.EventId == null)
                return null;

            var data = VkMessengerClient.ReadCallbackData(messageEvent.Payload);
            if (string.IsNullOrEmpty(data))
                return null;

            return new BotCallback
            {
                Messenger = MessengerType.Vk,
                UserId = messageEvent.UserId.Value.ToString(),
                ChatId = messageEvent.PeerId.Value.ToString(),
                Data = data,
                CallbackId = messageEvent.EventId,
                MessageId = messageEvent.ConversationMessageId?.ToString() ?? string.Empty,
                // ВКонтакте не присылает текст сообщения с кнопкой, обработчик учитывает это
                MessageText = string.Empty
            };
        }
    }
}
