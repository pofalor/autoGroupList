using GroupListNet.Core.src.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<bool> IsStartClassNotificationSentAsync(int studentId, int scheduleId, DateOnly date);
        /// <summary>
        /// Убирает из последовательности кому уже был отправлен отчёт
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        Task<int[]> ExceptSentReportAsync(int[] leaders, DateOnly date);
        Task RecordStartClassNotificationAsync(int studentId, int scheduleId, string text);
        Task RecordDailyReportSentAsync(int leaderStudentId, DateOnly date, string text);

        Task<IEnumerable<Notification>> GetUnsentStartClassNotificationsAsync();

        Task MarkAsSentAsync(Notification notification);

        Task DeleteNotifByScheduleIdsAsync(IEnumerable<int> scheduleIds);

        Task DeleteNotifByStudentIdsAsync(IEnumerable<int> studentIds);

        Task WriteSendingErrorAsync(Notification notification, string errorMessage);
    }
}
