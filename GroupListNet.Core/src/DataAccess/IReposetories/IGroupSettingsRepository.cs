using GroupListNet.Core.src.Entities;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface IGroupSettingsRepository : IRepository<GroupSettings>
    {
        /// <summary>
        /// Возвращает настройки группы, создавая запись с настройками по умолчанию, если её ещё нет
        /// </summary>
        Task<GroupSettings> GetOrCreateAsync();

        /// <summary>
        /// Делится ли группа на подгруппы
        /// </summary>
        Task<bool> HasSubgroupsAsync();

        /// <summary>
        /// Задаёт, делится ли группа на подгруппы
        /// </summary>
        Task SetHasSubgroupsAsync(bool hasSubgroups);
    }
}
