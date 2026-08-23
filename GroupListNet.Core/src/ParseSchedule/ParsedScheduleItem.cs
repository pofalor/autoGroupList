using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.ParseSchedule
{
    // Вспомогательный класс для хранения разобранного элемента расписания
    public class ParsedScheduleItem
    {
        public DayOfWeek? DayOfWeek { get; set; }
        public WeekType? WeekType { get; set; }
        public Subgroup? Subgroup { get; set; }
        public DateOnly? Date { get; set; } // Для конкретной даты
        public List<DateOnly> Dates { get; set; } = []; // Для нескольких конкретных дат
        public TimeOnly StartTime { get; set; }
        public string Type { get; set; } = string.Empty; // пр, лек, л.р.
        public string SubjectName { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public int? Building { get; set; }
    }
}
