using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.StringEnums;
using VkNet.Exception;
using VkNet.Model;

namespace GroupListNet.Core.src.Bot.Clients
{
    /// <summary>
    /// Транспорт бота для ВКонтакте
    /// </summary>
    public class VkMessengerClient : IMessengerClient
    {
        /// <summary>
        /// Всплывающее сообщение ВКонтакте показывает коротким
        /// </summary>
        private const int MaxSnackbarLength = 90;

        private readonly ILogger<VkMessengerClient> _logger;
        private readonly VkSettingsConfiguration _config;
        private readonly IVkApi? _api;

        public VkMessengerClient(ILogger<VkMessengerClient> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config.GetSection(VkSettingsConfiguration.VkSectionInConfig).Get<VkSettingsConfiguration>()
                ?? new VkSettingsConfiguration();

            if (!_config.IsConfigured)
            {
                _logger.LogWarning("Не задан {SectionName}:{SettingName}, бот ВКонтакте выключен.",
                    VkSettingsConfiguration.VkSectionInConfig, nameof(VkSettingsConfiguration.VkGroupToken));
                return;
            }

            var api = new VkApi(new ServiceCollection());
            api.Authorize(new ApiAuthParams { AccessToken = _config.VkGroupToken });
            _api = api;
        }

        public MessengerType Messenger => MessengerType.Vk;

        public bool IsEnabled => _api != null;

        /// <summary>
        /// Клиент библиотеки для приёма обновлений. Нужен только фоновой задаче ВКонтакте
        /// </summary>
        public IVkApi Api => _api
            ?? throw new InvalidOperationException("Бот ВКонтакте не настроен, клиент недоступен.");

        /// <summary>
        /// Айди сообщества, от имени которого работает бот.
        /// null, если в конфиге он не задан или задан не числом — тогда Long Poll не поднять
        /// </summary>
        public ulong? GroupId => ulong.TryParse(_config.VkGroupId, out var groupId) ? groupId : null;

        /// <summary>
        /// Название настройки с айди сообщества. Нужно, чтобы понятно пожаловаться в лог
        /// </summary>
        public static string GroupIdSettingName =>
            $"{VkSettingsConfiguration.VkSectionInConfig}:{nameof(VkSettingsConfiguration.VkGroupId)}";

        public async Task SendMessageAsync(string chatId, string text, BotKeyboard? keyboard = null, bool markdown = false)
        {
            if (_api == null)
                return;

            try
            {
                await _api.Messages.SendAsync(new MessagesSendParams
                {
                    PeerId = ParsePeerId(chatId),
                    Message = ToPlainText(text, markdown),
                    // random_id обязателен: по нему ВКонтакте отсекает повторную отправку
                    RandomId = Random.Shared.NextInt64(),
                    Keyboard = ToVkKeyboard(keyboard)
                });
            }
            catch (VkApiException ex)
            {
                throw ToSendException(ex);
            }
        }

        public async Task EditMessageAsync(string chatId, string messageId, string text, BotKeyboard? keyboard = null, bool markdown = false)
        {
            if (_api == null)
                return;

            try
            {
                await _api.Messages.EditAsync(new MessageEditParams
                {
                    PeerId = ParsePeerId(chatId),
                    ConversationMessageId = ParseConversationMessageId(messageId),
                    Message = ToPlainText(text, markdown),
                    Keyboard = ToVkKeyboard(keyboard)
                });
            }
            catch (VkApiException ex)
            {
                throw ToSendException(ex);
            }
        }

        public async Task RemoveKeyboardAsync(string chatId, string messageId, string? fallbackText = null)
        {
            if (_api == null)
                return;

            // ВКонтакте не умеет менять только клавиатуру: приходится переотправлять текст сообщения.
            // Если текста нет, оставляем кнопку как есть — повторное нажатие обработчик отработает повторно
            if (string.IsNullOrWhiteSpace(fallbackText))
            {
                _logger.LogDebug("Не убираем кнопки у сообщения {MessageId}: неизвестен его текст.", messageId);
                return;
            }

            await EditMessageAsync(chatId, messageId, fallbackText, BotKeyboard.Remove);
        }

        public async Task AnswerCallbackAsync(BotCallback callback, string? text = null, bool showAlert = false)
        {
            if (_api == null || string.IsNullOrEmpty(text))
                return;

            try
            {
                // Всплывающее сообщение — единственный способ ответить на нажатие во ВКонтакте,
                // отдельного «алерта», как в телеграме, там нет
                await _api.Messages.SendMessageEventAnswerAsync(
                    callback.CallbackId,
                    ParsePeerId(callback.UserId),
                    ParsePeerId(callback.ChatId),
                    new EventData
                    {
                        Type = MessageEventType.ShowSnackbar,
                        Text = VkKeyboardBuilder.Truncate(text, MaxSnackbarLength)
                    });
            }
            catch (VkApiException ex)
            {
                // Ответ на нажатие живёт недолго, просрочка не должна ронять обработку
                _logger.LogWarning(ex, "Не удалось ответить на нажатие кнопки {CallbackId} во ВКонтакте.", callback.CallbackId);
            }
        }

        /// <summary>
        /// Достаёт данные кнопки из полезной нагрузки ВКонтакте
        /// </summary>
        public static string ReadCallbackData(string? payload)
        {
            return VkKeyboardBuilder.ReadCallbackData(payload);
        }

        private static long ParsePeerId(string id)
        {
            return long.TryParse(id, out var parsed)
                ? parsed
                : throw new ArgumentException($"Некорректный айди ВКонтакте: {id}", nameof(id));
        }

        private static long ParseConversationMessageId(string messageId)
        {
            return long.TryParse(messageId, out var parsed)
                ? parsed
                : throw new ArgumentException($"Некорректный айди сообщения ВКонтакте: {messageId}", nameof(messageId));
        }

        /// <summary>
        /// ВКонтакте не понимает разметку, поэтому символы markdown убираем, чтобы они не лезли в текст
        /// </summary>
        private static string ToPlainText(string text, bool markdown)
        {
            return markdown ? text.Replace("*", string.Empty).Replace("_", string.Empty) : text;
        }

        private MessageKeyboard? ToVkKeyboard(BotKeyboard? keyboard)
        {
            return VkKeyboardBuilder.Build(keyboard, (buttonCount, maxButtons) =>
                _logger.LogWarning("Кнопок больше, чем вмещает клавиатура ВКонтакте ({ButtonCount} > {MaxButtons}), " +
                    "лишние не показаны.", buttonCount, maxButtons));
        }

        /// <summary>
        /// Приводит ошибку ВКонтакте к общему виду: запрет писать студенту — это не сбой отправки
        /// </summary>
        private static MessengerSendException ToSendException(VkApiException exception)
        {
            // 901 — сообщество не может писать первым, 902 — пользователь запретил сообщения от сообщества
            var isPermanent = exception.ErrorCode is 901 or 902 or 7 or 15;

            return new MessengerSendException(exception.Message, isPermanent, exception);
        }
    }
}
