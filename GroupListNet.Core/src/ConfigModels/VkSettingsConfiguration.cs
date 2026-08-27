namespace GroupListNet.Core.src.ConfigSectionModels
{
    public class VkSettingsConfiguration
    {
        /// <summary>
        /// Название секции конфигурации по умолчанию
        /// </summary>
        public const string VkSectionInConfig = "VkSettings";

        /// <summary>
        /// Ключ доступа сообщества с правом «Сообщения». Если пуст, бот ВКонтакте не запускается
        /// </summary>
        public string VkGroupToken { get; set; } = string.Empty;

        /// <summary>
        /// Айди сообщества, от имени которого работает бот. Нужен, чтобы получить сервер Long Poll
        /// </summary>
        public string VkGroupId { get; set; } = string.Empty;

        /// <summary>
        /// Айди администраторов во ВКонтакте
        /// </summary>
        public string AdminVkIds { get; set; } = string.Empty;

        /// <summary>
        /// Айди основных старост во ВКонтакте (тех, кто разворачивает систему).
        /// Такого старосту нельзя снять через бота, но он может назначать помощников
        /// </summary>
        public string LeaderVkIds { get; set; } = string.Empty;

        public string[] AdminVkIdsArray => SplitIds(AdminVkIds);

        public string[] LeaderVkIdsArray => SplitIds(LeaderVkIds);

        /// <summary>
        /// Настроен ли ВКонтакте. Без токена сообщества бот работать не может
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(VkGroupToken);

        /// <summary>
        /// Разбирает список айди из конфига, отбрасывая пустые значения и лишние пробелы
        /// </summary>
        private static string[] SplitIds(string ids)
        {
            return ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
