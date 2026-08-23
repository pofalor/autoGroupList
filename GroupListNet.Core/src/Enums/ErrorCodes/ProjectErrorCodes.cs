using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Resources.ErrorCodes;

namespace GroupListNet.Core.src.Enums.ErrorCodes
{
    public enum ProjectErrorCodes
    {
        /// <summary>
        /// Не удалось получить информацию о проектах
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(CannotGetProjects))]
        CannotGetProjects = WorkspaceErrorCodes.CannotGetMyWorkspaces + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Юзер не является членом рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(UserNotMemberWsp))]
        UserNotMemberWsp = CannotGetProjects + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. не задана ссылка на рабочее пространство
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(WorkspaceNotSet))]
        WorkspaceNotSet = UserNotMemberWsp + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. не задана ссылка на менеджера проектов
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(ProjectMgrNotSet))]
        ProjectMgrNotSet = WorkspaceNotSet + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. не задана ссылка на автора проекта
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(AuthorNotSet))]
        AuthorNotSet = ProjectMgrNotSet + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. не задана дата окончания проекта
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(EndDateNotSet))]
        EndDateNotSet = AuthorNotSet + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. дата начала проекта в будущем
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(StartDateInFuture))]
        StartDateInFuture = EndDateNotSet + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. имя проекта пустое
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(ProjectEmptyName))]
        ProjectEmptyName = StartDateInFuture + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. код проекта пустой
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(ProjectEmptyCode))]
        ProjectEmptyCode = ProjectEmptyName + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. проект с таким же кодом или именем существует
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(ProjectWithNameOrCodeExists))]
        ProjectWithNameOrCodeExists = ProjectEmptyCode + 1,

        /// <summary>
        /// Не удаётся создать проект, т.к. проджект менеджер не член рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(ProjectMgrNotWspMember))]
        ProjectMgrNotWspMember = ProjectWithNameOrCodeExists + 1,

        /// <summary>
        /// Не удаётся создать проект
        /// </summary>
        [ErrorMessage(typeof(ProjectErrorCodeResources), nameof(CannotCreateProject))]
        CannotCreateProject = ProjectMgrNotWspMember + 1,

        /// <summary>
        /// Не удаётся сделать запрос, т.к. нет прав у пользователя
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(AccessDenied))]
        AccessDenied = CannotCreateProject + 1,


        /// <summary>
        /// Не удалось получить кандидатов в проджект менеджеры
        /// </summary>
        [ErrorMessage(typeof(WorkspaceErrorCodeResources), nameof(CannotGetProjectMgrCandidates))]
        CannotGetProjectMgrCandidates = AccessDenied + 1,
    }
}
