namespace GroupListNet.Core.src.ConfigSectionModels
{
    public class TelegramSettingsConfiguration
    {
        /// <summary>
        /// Название секции конфигурации по умолчанию
        /// </summary>
        public const string TelegramSectionInConfig = "TelegramSettings";

        /// <summary>
        /// Телеграм айди админов в телеге
        /// </summary>
        public string AdminTelegramIds { get; set; } = string.Empty;

        /// <summary>
        /// Телеграм айди основных старост (тех, кто разворачивает систему).
        /// Такого старосту нельзя снять через бота, но он может назначать помощников
        /// </summary>
        public string LeaderTelegramIds { get; set; } = string.Empty;

        /// <summary>
        /// Токен телеграм бота
        /// </summary>
        public string TelegramBotToken { get; set; } = string.Empty;

        public string[] AdminTelegramIdsArray => SplitIds(AdminTelegramIds);

        public string[] LeaderTelegramIdsArray => SplitIds(LeaderTelegramIds);

        /// <summary>
        /// Разбирает список телеграм айди из конфига, отбрасывая пустые значения и лишние пробелы
        /// </summary>
        private static string[] SplitIds(string ids)
        {
            return ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
