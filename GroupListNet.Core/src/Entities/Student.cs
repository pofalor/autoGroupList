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
        /// <summary>
        /// Айди студента в телеграме. null означает, что мессенджер не привязан:
        /// именно на это опираются выборки незарегистрированных студентов
        /// </summary>
        public string? TelegramId { get; set; }
        /// <summary>
        /// Айди студента во ВКонтакте. Студент может привязать оба мессенджера и отмечаться в любом из них
        /// </summary>
        public string? VkId { get; set; }
        /// <summary>
        /// Если подгруппа не указана, значит студент ходит на занятия обеих подгрупп
        /// </summary>
        public Subgroup? Subgroup { get; set; }

        public string GetFullName()
        {
            return $"{SurName} {Name} {FatherName}";
        }

        /// <summary>
        /// Айди студента в конкретном мессенджере
        /// </summary>
        public string? GetMessengerId(MessengerType messenger)
        {
            return messenger switch
            {
                MessengerType.Telegram => TelegramId,
                MessengerType.Vk => VkId,
                _ => throw new ArgumentOutOfRangeException(nameof(messenger), messenger, "Неизвестный мессенджер.")
            };
        }

        public void SetMessengerId(MessengerType messenger, string? messengerId)
        {
            switch (messenger)
            {
                case MessengerType.Telegram:
                    TelegramId = messengerId;
                    break;
                case MessengerType.Vk:
                    VkId = messengerId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messenger), messenger, "Неизвестный мессенджер.");
            }
        }

        /// <summary>
        /// Зарегистрирован ли студент хотя бы в одном мессенджере
        /// </summary>
        public bool HasAnyMessenger()
        {
            return !string.IsNullOrWhiteSpace(TelegramId) || !string.IsNullOrWhiteSpace(VkId);
        }

        /// <summary>
        /// Мессенджеры, в которых студент зарегистрирован
        /// </summary>
        public IEnumerable<MessengerType> GetLinkedMessengers()
        {
            if (!string.IsNullOrWhiteSpace(TelegramId))
                yield return MessengerType.Telegram;

            if (!string.IsNullOrWhiteSpace(VkId))
                yield return MessengerType.Vk;
        }
    }
}
