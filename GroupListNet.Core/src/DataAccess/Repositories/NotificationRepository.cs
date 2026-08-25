using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<bool> IsStartClassNotificationSentAsync(int studentId, int scheduleId, DateOnly date, MessengerType messenger)
        {
            DateTime dateOnlyAsDateTime = date.ToDateTime(TimeOnly.MinValue);

            return await _dbSet.AnyAsync(n => n.StudentId == studentId &&
                                            n.ScheduleId == scheduleId &&
                                            n.NotificationType == NotificationType.StartClass &&
                                            n.Messenger == messenger &&
                                            n.ObjectCreateDate.Date == dateOnlyAsDateTime &&
                                            !n.IsDeleted);
        }

        public async Task<int[]> ExceptSentReportAsync(int[] leaders, DateOnly date, MessengerType messenger)
        {
            var leadersWithReport = await _dbSet
                .Where(n => n.NotificationType == NotificationType.EndDayReport)
                .Where(n => n.Messenger == messenger)
                .Where(n => DateOnly.FromDateTime(n.ObjectCreateDate) == date)
                .Where(n => !n.IsDeleted)
                .Select(x=> x.StudentId)
                .ToArrayAsync();

            return [.. leaders.Except(leadersWithReport)];
        }

        public async Task<IEnumerable<Notification>> GetUnsentNotificationsAsync(MessengerType messenger)
        {
            return await _dbSet
                .Include(x => x.Student)
                .Where(n => n.Messenger == messenger)
                .Where(n => !n.IsSent)
                .Where(n => !n.IsDeleted)
                .Where(n => string.IsNullOrEmpty(n.ErrorMessage))
                .ToListAsync();
        }

        public async Task RecordStartClassNotificationAsync(int studentId, int scheduleId, string text, MessengerType messenger)
        {
            var notification = new Notification
            {
                StudentId = studentId,
                ScheduleId = scheduleId,
                NotificationType = NotificationType.StartClass,
                Messenger = messenger,
                Text = text,
            };
            await AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task RecordDailyReportSentAsync(int leaderStudentId, DateOnly date, string text, MessengerType messenger)
        {
            var notification = new Notification
            {
                StudentId = leaderStudentId,
                NotificationType = NotificationType.EndDayReport,
                Messenger = messenger,
                Text = text,
            };
            await AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsSentAsync(Notification notification)
        {
            if (notification != null)
            {
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
                await UpdateAsync(notification); // Используем базовый метод UpdateAsync
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteNotifByScheduleIdsAsync(IEnumerable<int> scheduleIds)
        {
            // Удаление всех записей в таблице 
            var allNotifications = await _dbSet.Where(s => !s.IsDeleted)
                .Where(s=> s.ScheduleId.HasValue)
                .Where(s=> scheduleIds.Contains(s.ScheduleId.Value))
                .ToListAsync();

            foreach (var item in allNotifications)
            {
                item.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotifByStudentIdsAsync(IEnumerable<int> studentIds)
        {
            var allNotifications = await _dbSet.Where(s => !s.IsDeleted && studentIds.Contains(s.StudentId))
                .ToListAsync();

            foreach (var item in allNotifications)
            {
                item.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task WriteSendingErrorAsync(Notification notification, string errorMessage)
        {
            if (notification != null)
            {
                notification.ErrorMessage = errorMessage;
                await UpdateAsync(notification);
                await _context.SaveChangesAsync();
            }
        }
    }
}
