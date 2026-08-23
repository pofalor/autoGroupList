namespace GroupListNet.Core.src.ConfigSectionModels
{
    public class SecurityConfiguration
    {
        /// <summary>
        /// Название секции конфигурации по умолчанию
        /// </summary>
        public const string SecuritySectionInConfig = "Security";

        /// <summary>
        /// Кто выдал токен пользователю(по умолчанию - текущее приложение)
        /// </summary>
        public string AnonymousTokenRequest { get; set; } = string.Empty;
    }
}
