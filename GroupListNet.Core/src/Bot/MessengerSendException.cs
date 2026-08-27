namespace GroupListNet.Core.src.Bot
{
    /// <summary>
    /// Ошибка отправки сообщения, приведённая к общему виду.
    /// Нужна, чтобы отправщик уведомлений не знал про особенности конкретного мессенджера
    /// </summary>
    public class MessengerSendException : Exception
    {
        public MessengerSendException(string message, bool isPermanent, Exception? innerException = null)
            : base(message, innerException)
        {
            IsPermanent = isPermanent;
        }

        /// <summary>
        /// true — повторять отправку бессмысленно: пользователь заблокировал бота,
        /// запретил сообщения от сообщества или чат не найден
        /// </summary>
        public bool IsPermanent { get; }
    }
}
