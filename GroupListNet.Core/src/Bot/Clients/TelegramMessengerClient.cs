using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GroupListNet.Core.src.Bot.Clients
{
    /// <summary>
    /// Транспорт бота для телеграма
    /// </summary>
    public class TelegramMessengerClient : IMessengerClient
    {
        private readonly ILogger<TelegramMessengerClient> _logger;
        private readonly TelegramBotClient? _botClient;

        public TelegramMessengerClient(ILogger<TelegramMessengerClient> logger, IConfiguration config)
        {
            _logger = logger;

            var telegramConfig = config.GetSection(TelegramSettingsConfiguration.TelegramSectionInConfig)
                .Get<TelegramSettingsConfiguration>();

            var token = telegramConfig?.TelegramBotToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Не задан {SectionName}:{SettingName}, телеграм-бот выключен.",
                    TelegramSettingsConfiguration.TelegramSectionInConfig,
                    nameof(TelegramSettingsConfiguration.TelegramBotToken));
                return;
            }

            _botClient = new TelegramBotClient(token);
        }

        public MessengerType Messenger => MessengerType.Telegram;

        public bool IsEnabled => _botClient != null;

        /// <summary>
        /// Клиент библиотеки для приёма обновлений. Нужен только фоновой задаче телеграма
        /// </summary>
        public TelegramBotClient BotClient => _botClient
            ?? throw new InvalidOperationException("Телеграм-бот не настроен, клиент недоступен.");

        public async Task SendMessageAsync(string chatId, string text, BotKeyboard? keyboard = null, bool markdown = false)
        {
            if (_botClient == null)
                return;

            try
            {
                await _botClient.SendMessage(chatId, text, ToParseMode(markdown), replyMarkup: ToReplyMarkup(keyboard));
            }
            catch (ApiRequestException ex)
            {
                throw ToSendException(ex);
            }
        }

        public async Task EditMessageAsync(string chatId, string messageId, string text, BotKeyboard? keyboard = null, bool markdown = false)
        {
            if (_botClient == null)
                return;

            try
            {
                await _botClient.EditMessageText(chatId, ParseMessageId(messageId), text,
                    parseMode: ToParseMode(markdown), replyMarkup: ToInlineMarkup(keyboard));
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                // Ок, сообщение не изменилось
            }
            catch (ApiRequestException ex)
            {
                throw ToSendException(ex);
            }
        }

        public async Task RemoveKeyboardAsync(string chatId, string messageId, string? fallbackText = null)
        {
            if (_botClient == null)
                return;

            try
            {
                await _botClient.EditMessageReplyMarkup(chatId, ParseMessageId(messageId), replyMarkup: null);
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                // Ок, кнопок и так уже нет
            }
            catch (ApiRequestException ex)
            {
                throw ToSendException(ex);
            }
        }

        public async Task AnswerCallbackAsync(BotCallback callback, string? text = null, bool showAlert = false)
        {
            if (_botClient == null)
                return;

            try
            {
                await _botClient.AnswerCallbackQuery(callback.CallbackId, text, showAlert: showAlert);
            }
            catch (ApiRequestException ex)
            {
                // Ответ на нажатие живёт меньше минуты, просрочка не должна ронять обработку
                _logger.LogWarning(ex, "Не удалось ответить на нажатие кнопки {CallbackId}.", callback.CallbackId);
            }
        }

        private static ParseMode ToParseMode(bool markdown)
        {
            return markdown ? ParseMode.Markdown : ParseMode.None;
        }

        private static int ParseMessageId(string messageId)
        {
            return int.TryParse(messageId, out var parsed)
                ? parsed
                : throw new ArgumentException($"Некорректный id сообщения телеграма: {messageId}", nameof(messageId));
        }

        private static ReplyMarkup? ToReplyMarkup(BotKeyboard? keyboard)
        {
            if (keyboard == null)
                return null;

            if (keyboard.IsInline)
                return ToInlineMarkup(keyboard);

            // Пустая клавиатура меню означает «убрать меню»
            if (keyboard.Rows.Count == 0)
                return new ReplyKeyboardRemove();

            var rows = keyboard.Rows
                .Select(row => row.Select(button => new KeyboardButton(button.Text)).ToArray())
                .ToArray();

            return new ReplyKeyboardMarkup(rows) { ResizeKeyboard = true };
        }

        private static InlineKeyboardMarkup? ToInlineMarkup(BotKeyboard? keyboard)
        {
            if (keyboard == null || !keyboard.IsInline || keyboard.Rows.Count == 0)
                return null;

            var rows = keyboard.Rows
                .Select(row => row
                    .Select(button => new InlineKeyboardButton(button.Text) { CallbackData = button.Data })
                    .ToArray())
                .ToArray();

            return new InlineKeyboardMarkup(rows);
        }

        /// <summary>
        /// Приводит ошибку телеграма к общему виду: заблокировавший бота студент — это не сбой отправки
        /// </summary>
        private static MessengerSendException ToSendException(ApiRequestException exception)
        {
            var isPermanent = exception.ErrorCode == 403
                || exception.Message.Contains("bot was blocked by the user")
                || exception.Message.Contains("chat not found");

            return new MessengerSendException(exception.Message, isPermanent, exception);
        }
    }
}
