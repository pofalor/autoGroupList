using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum SystemErrorCodes
    {
        /// <summary>
        /// Системная ошибка
        /// </summary>
        [ErrorMessage(typeof(SystemErrorCodeResources), nameof(SystemError))]
        SystemError = -1,

        /// <summary>
        /// Недействительный запрос
        /// </summary>
        [ErrorMessage(typeof(SystemErrorCodeResources), nameof(InvalidRequest))]
        InvalidRequest = 0,

        /// <summary>
        /// Отказано в доступе
        /// </summary>
        [ErrorMessage(typeof(SystemErrorCodeResources), nameof(AccessDenied))]
        AccessDenied = 403
    }
}