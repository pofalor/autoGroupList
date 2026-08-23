namespace GroupListNet.Core.src.Entities
{
    public class Attendance : PersistentEntity
    {
        /// <summary>
        /// Дата когда студент отметился (со временем)
        /// </summary>
        public DateTime Date { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; } = null!;
    }
}