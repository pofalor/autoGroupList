using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Entities
{
    public class Schedule : PersistentEntity
    {
        /// <summary>
        /// Если не указано, значит независимо от подгруппы придут уведы
        /// </summary>
        public Subgroup? Subgroup { get; set; }
        public WeekType? WeekType { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public DateOnly? Date {  get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public ClassType ClassType { get; set; }
        public string Room { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
    }
}
