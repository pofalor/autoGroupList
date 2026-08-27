using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface IStudentRepository : IRepository<Student>
    {
        /// <summary>
        /// Студент по его айди в конкретном мессенджере
        /// </summary>
        Task<Student?> GetByMessengerIdAsync(MessengerType messenger, string messengerId);
        Task<Student?> GetByNumberInGroupAsync(int number);

        /// <summary>
        /// Занят ли номер в списке группы кем-то, кто уже зарегистрировался в этом мессенджере
        /// </summary>
        Task<bool> IsNumberTakenAsync(int number, MessengerType messenger);

        /// <summary>
        /// Студенты, зарегистрированные хотя бы в одном мессенджере. Им шлём уведомления о парах
        /// </summary>
        Task<IEnumerable<Student>> GetStudentsWithAnyMessengerAsync();

        Task UpdateStudentSubgroupAsync(MessengerType messenger, string messengerId, Subgroup? subgroup);

        /// <summary>
        /// Студенты, ещё не привязавшие этот мессенджер. Из них выбирает свой номер тот, кто регистрируется.
        /// Студент, зарегистрированный в телеграме, попадает сюда для ВКонтакте — так он привязывает второй мессенджер
        /// </summary>
        Task<IEnumerable<Student>> GetStudentsWithoutAccountAsync(MessengerType messenger);

        Task<IEnumerable<Student>> DeleteAllAsync();

        /// <summary>
        /// Убирает подгруппу у всех студентов. Нужно, когда староста выключил деление группы на подгруппы
        /// </summary>
        Task ResetSubgroupsAsync();
    }
}
