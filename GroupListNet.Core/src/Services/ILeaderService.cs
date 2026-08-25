using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Services
{
    /// <summary>
    /// Управление правами старосты: основной староста задаётся в конфиге, помощников он назначает через бота
    /// </summary>
    public interface ILeaderService
    {
        /// <summary>
        /// Доступны ли пользователю команды старосты (основной староста из конфига или назначенный помощник)
        /// </summary>
        Task<bool> IsLeaderAsync(MessengerType messenger, string? messengerId);

        /// <summary>
        /// Основной староста задан в конфиге, снять его через бота нельзя
        /// </summary>
        bool IsMainLeader(MessengerType messenger, string? messengerId);

        /// <summary>
        /// Основной ли это староста с учётом всех привязанных мессенджеров студента
        /// </summary>
        bool IsMainLeader(Student student);

        /// <summary>
        /// Администраторы задаются в конфиге отдельно для каждого мессенджера
        /// </summary>
        bool IsAdmin(MessengerType messenger, string? messengerId);

        /// <summary>
        /// Заводит записи старост для айди из конфига, если такие студенты уже зарегистрированы.
        /// Вызывается после регистрации студента и после обновления списка группы
        /// </summary>
        Task SyncMainLeadersAsync();

        /// <summary>
        /// Студенты, которым сейчас доступны команды старосты
        /// </summary>
        Task<IReadOnlyCollection<Student>> GetLeadersAsync();

        /// <summary>
        /// Зарегистрированные студенты, которых можно назначить помощниками
        /// </summary>
        Task<IReadOnlyCollection<Student>> GetAssistantCandidatesAsync();

        /// <summary>
        /// Помощники, которых можно снять (основные старосты из конфига сюда не попадают)
        /// </summary>
        Task<IReadOnlyCollection<Student>> GetRemovableAssistantsAsync();

        /// <summary>
        /// Назначает студента помощником старосты
        /// </summary>
        Task<IDataResult<Student>> AddAssistantAsync(int studentId);

        /// <summary>
        /// Снимает с студента права помощника старосты
        /// </summary>
        Task<IDataResult<Student>> RemoveAssistantAsync(int studentId);
    }
}
