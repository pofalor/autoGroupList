using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum IssueErrorCodes
    {
        /// <summary>
        /// Не удалось получить информацию о задачах
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(CannotGetIssues))]
        CannotGetIssues = ProjectErrorCodes.CannotGetProjects + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Юзер не является членом рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(UserNotMemberWsp))]
        UserNotMemberWsp = CannotGetIssues + 1,

        /// <summary>
        /// Не удается создать задачу с не заданным проектом
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(ProjectNotSet))]
        ProjectNotSet = UserNotMemberWsp + 1,

        /// <summary>
        /// Не удаётся создать задачу с не заданным автором
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(AuthorNotSet))]
        AuthorNotSet = ProjectNotSet + 1,

        /// <summary>
        /// Не удаётся создать задачу с пустым именем
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(EmptyName))]
        EmptyName = AuthorNotSet + 1,

        /// <summary>
        /// Не удаётся создать задачу
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(CannotCreateIssue))]
        CannotCreateIssue = EmptyName + 1,

        /// <summary>
        /// Не удаётся создать списывание времени с не заданным полем задачи
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(IssueNotSet))]
        IssueNotSet = CannotCreateIssue + 1,

        /// <summary>
        /// Не удаётся создать списывание часов
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(CannotCreateTimeTrack))]
        CannotCreateTimeTrack = IssueNotSet + 1,

        /// <summary>
        /// Не удаётся трекнуть часы с нулевым значением
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(TimeTrackIsZero))]
        TimeTrackIsZero = CannotCreateTimeTrack + 1,

        /// <summary>
        /// Не удается создать задачу с пустым описанием
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(EmptyDescr))]
        EmptyDescr = TimeTrackIsZero + 1,

        /// <summary>
        /// Не удается создать задачу с неправильным типом
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(IssueTypeInvalid))]
        IssueTypeInvalid = EmptyDescr + 1,

        /// <summary>
        /// Не удается создать задачу с неправильным статусом
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(IssueStatusInvalid))]
        IssueStatusInvalid = IssueTypeInvalid + 1,

        /// <summary>
        /// Не удается создать задачу с неправильным приоритетом
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(IssuePriorityInvalid))]
        IssuePriorityInvalid = IssueStatusInvalid + 1,

        /// <summary>
        /// Неправильно задан исполнитель задачи
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(IssueAssigneeInvalid))]
        IssueAssigneeInvalid = IssuePriorityInvalid + 1,

        /// <summary>
        /// Исполнитель задачи не является членом рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(AssigneeNotMemberWsp))]
        AssigneeNotMemberWsp = IssueAssigneeInvalid + 1,

        /// <summary>
        /// Не удаётся сделать запрос, т.к. нет прав у пользователя
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(AccessDenied))]
        AccessDenied = AssigneeNotMemberWsp + 1,

        /// <summary>
        /// Не удаётся списать часы, т.к. не задана дата начала
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(TrackDateNotSet))]
        TrackDateNotSet = AccessDenied + 1,

        /// <summary>
        /// Не удаётся списать часы, т.к. дата начала в будущем
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(TrackDateInFuture))]
        TrackDateInFuture = TrackDateNotSet + 1
    }
}
