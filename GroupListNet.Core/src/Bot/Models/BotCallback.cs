using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Bot.Models
{
    /// <summary>
    /// Нажатие на кнопку под сообщением, не зависящее от мессенджера
    /// </summary>
    public class BotCallback
    {
        public MessengerType Messenger { get; set; }

        public string UserId { get; set; } = null!;

        public string ChatId { get; set; } = null!;

        /// <summary>
        /// Данные кнопки, например attend_12_345
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор самого нажатия, на которое надо ответить.
        /// В телеграме это CallbackQuery.Id, во ВКонтакте — event_id
        /// </summary>
        public string CallbackId { get; set; } = null!;

        /// <summary>
        /// Идентификатор сообщения с кнопкой.
        /// В телеграме это message_id, во ВКонтакте — conversation_message_id
        /// </summary>
        public string MessageId { get; set; } = null!;

        /// <summary>
        /// Текст сообщения с кнопкой. Во ВКонтакте не приходит, поэтому может быть пустым
        /// </summary>
        public string MessageText { get; set; } = string.Empty;
    }
}
