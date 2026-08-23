using System.Globalization;
using System.Resources;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    /// <summary>
    /// Указывает откуда брать сообщение об ошибке.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ErrorMessageAttribute : Attribute
    {
        private readonly string _resourceKey;
        private readonly ResourceManager _resource;

        /// <summary>
        /// Инициализирует экземпляр класса ErrorMessageAttribute с указанными значениями resourceKey и resourceType
        /// </summary>
        /// <param name="resourceType">Тип ресурсного файла, в котором хранятся сообщения об ошибках</param>
        /// <param name="resourceKey">Ключ ресурсного файла в котором хранится сообщение об ошибке</param>
        public ErrorMessageAttribute(Type resourceType, string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                throw new ArgumentException("resourceKey not filled");
            }
            _resource = new ResourceManager(resourceType);
            _resourceKey = resourceKey;
        }

        /// <summary>Получает сообщение об ошибке или resourceKey если оно не найдено в ресурсном файле</summary>
        public string GetErrorMessage()
        {
            var resorceValue = _resource.GetString(_resourceKey, new CultureInfo("ru-RU"));
            return string.IsNullOrEmpty(resorceValue) ? string.Format("[[{0}]]", _resourceKey) : resorceValue;
        }
    }
}
