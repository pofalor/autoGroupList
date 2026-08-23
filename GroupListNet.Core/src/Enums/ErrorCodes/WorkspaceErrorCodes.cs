using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum WorkspaceErrorCodes
    {
        /// <summary>
        /// Не удалось получить информацию о рабочих пространствах
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotGetMyWorkspaces))]
        CannotGetMyWorkspaces = UserErrorCodes.CannotGetUser + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Значение страны не задано
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CountryNull))]
        CountryNull = CannotGetMyWorkspaces + 1,

        /// <summary>
        /// Значение инн не задано
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(INNNull))]
        INNNull = CountryNull + 1,

        /// <summary>
        /// Значение адреса не задано
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(AddressNull))]
        AddressNull = INNNull + 1,

        /// <summary>
        /// Значение даты регистрации не задано
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(RegistrationDateNull))]
        RegistrationDateNull = AddressNull + 1,

        /// <summary>
        /// Компания с таким инн уже существует
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CompanyWithInnAlreadyExists))]
        CompanyWithInnAlreadyExists = RegistrationDateNull + 1,

        /// <summary>
        /// Компания с теми же данными существует
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CompanyWithDataAlreadyExists))]
        CompanyWithDataAlreadyExists = CompanyWithInnAlreadyExists + 1,

        /// <summary>
        /// Компания с тем же именем существует
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CompanyWithNameAlreadyExists))]
        CompanyWithNameAlreadyExists = CompanyWithDataAlreadyExists + 1,

        /// <summary>
        /// Можно создать только один личный воркспейс
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CanCreateOnlyOnePersonalWorkspace))]
        CanCreateOnlyOnePersonalWorkspace = CompanyWithNameAlreadyExists + 1,

        /// <summary>
        /// Не удаётся создать или изменить сущность(вокрспейс или инвайт)
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotCreateOrEditWorkspace))]
        CannotCreateOrEditWorkspace = CanCreateOnlyOnePersonalWorkspace + 1,

        /// <summary>
        /// Не удаётся сделать запрос, т.к. нет прав у пользователя
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(AccessDenied))]
        AccessDenied = CannotCreateOrEditWorkspace + 1,


        /// <summary>
        /// Не удаётся получить данные о приглашениях юзера
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotGetWpsRequests))]
        CannotGetWpsRequests = AccessDenied + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана ссылка на рабочее пространство
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(WorkspaceNotSet))]
        WorkspaceNotSet = CannotGetWpsRequests + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана ссылка на юзера
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(WpsInviteUserIdNotSet))]
        WpsInviteUserIdNotSet = WorkspaceNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана ссылка на инвайтера
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(WpsInviterIdNotSet))]
        WpsInviterIdNotSet = WpsInviteUserIdNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана дата запроса
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(WpsInviteReqDateNotSet))]
        WpsInviteReqDateNotSet = WpsInviterIdNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. дата запроса в будущем
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(WpsInviteReqDateFuture))]
        WpsInviteReqDateFuture = WpsInviteReqDateNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. такого воркспейса не существует
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(WpsForInviteNotExists))]
        WpsForInviteNotExists = WpsInviteReqDateFuture + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. инвайт уже создан
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(ActiveInviteAlreadyExists))]
        ActiveInviteAlreadyExists = WpsForInviteNotExists + 1,

        /// <summary>
        /// Не удаётся создать или изменить инвайт
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotCreateOrEditInviteWsp))]
        CannotCreateOrEditInviteWsp = ActiveInviteAlreadyExists + 1,

        /// <summary>
        /// Юзер уже член рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(UserAlreadyInWsp))]
        UserAlreadyInWsp = CannotCreateOrEditInviteWsp + 1,

        /// <summary>
        /// Юзер в таком рабочем пространстве не найден
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(UserNotFoundInWsp))]
        UserNotFoundInWsp = UserAlreadyInWsp + 1,

        /// <summary>
        /// Юзер для инвайта не найден
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotFindUserForInvite))]
        CannotFindUserForInvite = UserNotFoundInWsp + 1,

        /// <summary>
        /// Не удалось проверить является ли юзер владельцем организации
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotCheckOwner))]
        CannotCheckOwner = CannotFindUserForInvite + 1,

        /// <summary>
        /// Не задано айди инвайта при прниятии инвайта
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(InviteIdNotSet))]
        InviteIdNotSet = CannotCheckOwner + 1,

        /// <summary>
        /// Неверный статус при принятии инвайта
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(InvalidStatusInvite))]
        InvalidStatusInvite = InviteIdNotSet + 1,

        /// <summary>
        /// Активный инвайт в базе не найден
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(InviteNotExists))]
        InviteNotExists = InvalidStatusInvite + 1,

        /// <summary>
        /// Не удаётся принять инвайт
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotAcceptInviteWsp))]
        CannotAcceptInviteWsp = InviteNotExists + 1,

        /// <summary>
        /// Не задан статус проверки администратором
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(ReviewStatusNull))]
        ReviewStatusNull = CannotAcceptInviteWsp + 1,

        /// <summary>
        /// Статус проверки администратором не валиден
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(ReviewStatusWrong))]
        ReviewStatusWrong = ReviewStatusNull + 1,

        /// <summary>
        /// Не удалось получить рабочие пространства для проверки администратором
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotGetWspForAdmin))]
        CannotGetWspForAdmin = ReviewStatusWrong + 1,

        /// <summary>
        /// Не задан статус для проверки администратором
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(ReviewStatusNotSet))]
        ReviewStatusNotSet = CannotGetWspForAdmin + 1,

        /// <summary>
        /// Невалидный статус отправлен с фронта для апрува
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(ReviewStatusInvalid))]
        ReviewStatusInvalid = ReviewStatusNotSet + 1,

        /// <summary>
        /// Не найдено рабочее пространство для проставления Review status
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(WorkspaceNotFound))]
        WorkspaceNotFound = ReviewStatusInvalid + 1,

        /// <summary>
        /// Не удалось изменить ReviewStatus 
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotChangeReviewStatus))]
        CannotChangeReviewStatus = WorkspaceNotFound + 1,
    }
}
