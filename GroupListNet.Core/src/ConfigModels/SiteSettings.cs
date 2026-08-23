namespace GroupListNet.Core.src.ConfigModels
{
    public class SiteSettings
    {
        /// <summary>
        /// Название секции конфигурации по умолчанию
        /// </summary>
        public const string SiteSettingsSectionInConfig = "SiteSettings";

        /// <summary>
        /// Отмечать ли посещаемость на сайте. Если false, бот только собирает отметки в своей базе
        /// </summary>
        public bool MarkAttendanceOnSite { get; set; } = true;

        public string BaseUrl { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty; // URL начальной страницы, где кнопка "Войти"
        public string AttendancePageUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int SiteTimeOut { get; set; }

        public double TaskTimeoutMinutes { get; set; }

        /// <summary>
        /// Путь до браузера Chrome/Chromium. Если не задан, используется браузер по умолчанию.
        /// Нужен в Docker, где стоит системный chromium
        /// </summary>
        public string ChromeBinaryPath { get; set; } = string.Empty;

        /// <summary>
        /// Каталог с chromedriver. Если не задан, берётся драйвер, положенный рядом с приложением
        /// </summary>
        public string ChromeDriverDirectory { get; set; } = string.Empty;
    }
}
