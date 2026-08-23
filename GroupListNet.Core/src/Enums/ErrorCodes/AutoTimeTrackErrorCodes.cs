using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum AutoTimeTrackErrorCodes
    {
        /// <summary>
        /// Не удаётся создать списывание времени с не заданным полем задачи
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(IssueNotSet))]
        IssueNotSet = IssueErrorCodes.CannotGetIssues + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Автоматический трек уже существует
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(AutoTrackExists))]
        AutoTrackExists = IssueNotSet + 1,

        /// <summary>
        /// При автоматическом трекинге пришло неверное значение
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(TimeSpentInvalid))]
        TimeSpentInvalid = AutoTrackExists + 1,

        /// <summary>
        /// Дата начала трекинга в будущем
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(DateBeginInFuture))]
        DateBeginInFuture = TimeSpentInvalid + 1,

        /// <summary>
        /// Не удаётся получить активный трек
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(CannotGetAutoTrack))]
        CannotGetAutoTrack = DateBeginInFuture + 1,

        /// <summary>
        /// Не удаётся начать автоматический трек времени
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(CannotStartAutoTrack))]
        CannotStartAutoTrack = CannotGetAutoTrack + 1,

        /// <summary>
        /// Не удаётся сделать запрос, т.к. нет прав у пользователя
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(AccessDenied))]
        AccessDenied = CannotStartAutoTrack + 1,

        /// <summary>
        /// Юзер не является членом рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(UserNotMemberWsp))]
        UserNotMemberWsp = AccessDenied + 1,

        /// <summary>
        /// Текущий пользователь не является исполнителем задачи, по которой начинается авто трекинг
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(InalidAssignee))]
        InalidAssignee = UserNotMemberWsp + 1,

        /// <summary>
        /// Статус задачи, по которой начинается авто трекинг, является не валидным
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(InvalidIssueStatus))]
        InvalidIssueStatus = InalidAssignee + 1,

        /// <summary>
        /// Не удалось найти активный трек
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(CannotFindActiveTrack))]
        CannotFindActiveTrack = InvalidIssueStatus + 1,

        /// <summary>
        /// Не удаётся начать автоматический трек времени
        /// </summary>
        [ErrorMessage(typeof(AutoTimeTrackErrorCodeResources), nameof(CannotStopAutoTrack))]
        CannotStopAutoTrack = CannotFindActiveTrack + 1,
    }
}
