using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum BaseErrorCodes
    {
        /// <summary>
        /// Ошибка получения эл-ов
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(GetItemsError))]
        GetItemsError = SystemErrorCodes.InvalidRequest + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Ошибка в модели
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(ModelInvalid))]
        ModelInvalid = GetItemsError + 1,

        /// <summary>
        /// Не удаётся удалить элемент
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(DeleteItemError))]
        DeleteItemError = ModelInvalid + 1,

        /// <summary>
        /// Ошибка в модели
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(CreateItemError))]
        CreateItemError = DeleteItemError + 1,

        /// <summary>
        /// Ошибка получения эл-а
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(GetItemError))]
        GetItemError = CreateItemError + 1,
    }
}
