using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<bool> IsStartClassNotificationSentAsync(int studentId, int scheduleId, DateOnly date, MessengerType messenger);
        /// <summary>
        /// Убирает из последовательности кому уже был отправлен отчёт
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        Task<int[]> ExceptSentReportAsync(int[] leaders, DateOnly date, MessengerType messenger);
        Task RecordStartClassNotificationAsync(int studentId, int scheduleId, string text, MessengerType messenger);
        Task RecordDailyReportSentAsync(int leaderStudentId, DateOnly date, string text, MessengerType messenger);

        /// <summary>
        /// Неотправленные уведомления конкретного мессенджера. Каждый бот забирает только свои,
        /// иначе студенту с двумя привязками сообщение уйдёт дважды в один мессенджер
        /// </summary>
        Task<IEnumerable<Notification>> GetUnsentNotificationsAsync(MessengerType messenger);

        Task MarkAsSentAsync(Notification notification);

        Task DeleteNotifByScheduleIdsAsync(IEnumerable<int> scheduleIds);

        Task DeleteNotifByStudentIdsAsync(IEnumerable<int> studentIds);

        Task WriteSendingErrorAsync(Notification notification, string errorMessage);
    }
}
