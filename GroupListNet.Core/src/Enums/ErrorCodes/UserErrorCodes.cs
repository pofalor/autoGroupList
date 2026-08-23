using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum UserErrorCodes
    {
        /// <summary>
        /// Не удалось получить юзера
        /// </summary>
        [ErrorMessage(typeof(UserErrorCodeResources), nameof(CannotGetUser))]
        CannotGetUser = SosErrorCodes.RoleNameNullError + ErrorConstants.EnumErrorCodeCount,
    }
}
