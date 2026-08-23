using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Entities
{
    public class Student : PersistentEntity
    {
        public string Name { get; set; } = null!;
        /// <summary>
        /// Отчество
        /// </summary>
        public string FatherName { get; set; } = null!;
        /// <summary>
        /// Фамилия
        /// </summary>
        public string SurName { get; set; } = null!;
        public int NumberInGroup { get; set; }
        public string? TelegramId { get; set; } = string.Empty;
        /// <summary>
        /// Если подгруппа не указана, значит студент ходит на занятия обеих подгрупп
        /// </summary>
        public Subgroup? Subgroup { get; set; }

        public string GetFullName()
        {
            return $"{SurName} {Name} {FatherName}";
        }
    }

}
