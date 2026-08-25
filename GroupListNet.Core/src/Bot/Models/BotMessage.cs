using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Bot.Models
{
    /// <summary>
    /// Текстовое сообщение пользователя, не зависящее от мессенджера
    /// </summary>
    public class BotMessage
    {
        public MessengerType Messenger { get; set; }

        /// <summary>
        /// Идентификатор пользователя в мессенджере. Именно он привязывается к студенту
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Идентификатор диалога, куда отправлять ответ.
        /// В телеграме это chat id, во ВКонтакте — peer id
        /// </summary>
        public string ChatId { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
    }
}
