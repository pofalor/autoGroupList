using GroupListNet.Core.src.Bot;
using GroupListNet.Core.src.Bot.Clients;
using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GroupListNet.Core.src.BackgroundJobs
{
    /// <summary>
    /// Приём сообщений из телеграма. Вся логика команд общая и живёт в <see cref="BotUpdateHandler"/>
    /// </summary>
    public class TelegramBotBackgroundJob : BackgroundService
    {
        private readonly ILogger<TelegramBotBackgroundJob> _logger;
        private readonly TelegramMessengerClient _client;
        private readonly BotUpdateHandler _handler;

        public TelegramBotBackgroundJob(ILogger<TelegramBotBackgroundJob> logger,
            TelegramMessengerClient client,
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
                _logger.LogWarning("Телеграм-бот не настроен, {ClassName} не запущен.", nameof(TelegramBotBackgroundJob));
                return;
            }

            // Подписка на события сама запускает получение обновлений long polling'ом
            _client.BotClient.OnUpdate += async (update) =>
            {
                try
                {
                    if (update.Message != null)
                    {
                        var message = ToBotMessage(update.Message);
                        if (message != null)
                            await _handler.HandleMessageAsync(_client, message);
                    }
                    else if (update.CallbackQuery != null)
                    {
                        var callback = ToBotCallback(update.CallbackQuery);
                        if (callback != null)
                            await _handler.HandleCallbackAsync(_client, callback);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке обновления телеграма.");
                }
            };

            _logger.LogInformation("Телеграм-бот начал приём сообщений.");

            try
            {
                // Обновления приходят событиями от клиента библиотеки, задаче остаётся дождаться остановки
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Приложение останавливается, это штатное завершение
            }
        }

        private static BotMessage? ToBotMessage(Message message)
        {
            if (message.Text == null || message.From == null)
                return null;

            return new BotMessage
            {
                Messenger = MessengerType.Telegram,
                UserId = message.From.Id.ToString(),
                ChatId = message.Chat.Id.ToString(),
                Text = message.Text
            };
        }

        private static BotCallback? ToBotCallback(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Data == null || callbackQuery.Message == null)
                return null;

            return new BotCallback
            {
                Messenger = MessengerType.Telegram,
                UserId = callbackQuery.From.Id.ToString(),
                ChatId = callbackQuery.Message.Chat.Id.ToString(),
                Data = callbackQuery.Data,
                CallbackId = callbackQuery.Id,
                MessageId = callbackQuery.Message.MessageId.ToString(),
                MessageText = callbackQuery.Message.Text ?? string.Empty
            };
        }
    }
}
