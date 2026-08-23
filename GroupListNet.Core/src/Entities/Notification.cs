using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Entities
{
    public class Notification : PersistentEntity
    {
        public NotificationType NotificationType { get; set; }
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
