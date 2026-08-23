using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum SosErrorCodes
    {
        /// <summary>
        /// Пришло пустое имя роли
        /// </summary>
        [ErrorMessage(typeof(SosErrorCodeResources), nameof(RoleNameNullError))]
        RoleNameNullError = AuthenticationErrorCodes.UserAlreadyExists + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Такая роль уже существует
        /// </summary>
        [ErrorMessage(typeof(SosErrorCodeResources), nameof(RoleAlreadyExists))]
        RoleAlreadyExists = RoleNameNullError + 1,

        /// <summary>
        /// Ошибка при создании роли
        /// </summary>
        [ErrorMessage(typeof(SosErrorCodeResources), nameof(RoleCreationError))]
        RoleCreationError = RoleAlreadyExists + 1,

        /// <summary>
        /// Неправильный токен
        /// </summary>
        [ErrorMessage(typeof(SosErrorCodeResources), nameof(InvalidToken))]
        InvalidToken = RoleCreationError + 1,

        /// <summary>
        /// Роль не существует
        /// </summary>
        [ErrorMessage(typeof(SosErrorCodeResources), nameof(RoleNotExists))]
        RoleNotExists = InvalidToken + 1,

        /// <summary>
        /// Пользователь не существует
        /// </summary>
        [ErrorMessage(typeof(SosErrorCodeResources), nameof(UserNotExists))]
        UserNotExists = RoleNotExists + 1,

        /// <summary>
        /// Ошибка при добавлении роли
        /// </summary>
        [ErrorMessage(typeof(SosErrorCodeResources), nameof(RoleAddingError))]
        RoleAddingError = UserNotExists + 1,
    }
}
