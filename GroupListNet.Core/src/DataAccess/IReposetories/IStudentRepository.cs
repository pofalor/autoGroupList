using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface IStudentRepository : IRepository<Student>
    {
        Task<Student?> GetByTelegramIdAsync(string telegramId);
        Task<Student?> GetByNumberInGroupAsync(int number);
        Task<bool> IsNumberTakenAsync(int number);
        Task<IEnumerable<Student>> GetStudentsWithTelegramAsync();
        Task UpdateStudentSubgroupAsync(string telegramId, Subgroup? subgroup);
        Task<IEnumerable<Student>> GetStudentsWithoutTelegramAsync();
        Task<IEnumerable<Student>> DeleteAllAsync();

        /// <summary>
        /// Убирает подгруппу у всех студентов. Нужно, когда староста выключил деление группы на подгруппы
        /// </summary>
        Task ResetSubgroupsAsync();
    }
}
