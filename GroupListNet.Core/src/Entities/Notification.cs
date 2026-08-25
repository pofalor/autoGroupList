using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Entities
{
    public class Notification : PersistentEntity
    {
        public NotificationType NotificationType { get; set; }

        /// <summary>
        /// Мессенджер, в который надо отправить уведомление. У студента с двумя привязками
        /// на одно занятие создаётся по уведомлению на каждый мессенджер
        /// </summary>
        public MessengerType Messenger { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
        public string Text { get; set; } = null!;

        /// <summary>
        /// В отчёте старосте проставляется айди старосты
        /// </summary>
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int? ScheduleId { get; set; }
        public Schedule? Schedule { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
