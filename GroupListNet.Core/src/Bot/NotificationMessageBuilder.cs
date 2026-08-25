using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Entities.Utils;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Bot
{
    /// <summary>
    /// Собирает текст уведомления в момент отправки. В базе лежит только сам факт уведомления,
    /// а содержимое выводится из расписания, предмета и отметок
    /// </summary>
    public class NotificationMessageBuilder
    {
        private readonly IAttendanceRepository _attendanceRepository;

        public NotificationMessageBuilder(IAttendanceRepository attendanceRepository)
        {
            _attendanceRepository = attendanceRepository;
        }

        /// <summary>
        /// Готовит сообщение к отправке. null означает, что собрать его не из чего —
        /// в штатной работе так быть не должно, это защита от несогласованных данных
        /// </summary>
        public async Task<(string Text, BotKeyboard? Keyboard)?> BuildAsync(Notification notification)
        {
            return notification.NotificationType switch
            {
                NotificationType.StartClass => BuildStartClass(notification),
                NotificationType.EndDayReport => await BuildEndDayReportAsync(notification),
                _ => null
            };
        }

        private static (string Text, BotKeyboard? Keyboard)? BuildStartClass(Notification notification)
        {
            var scheduleItem = notification.Schedule;
            if (scheduleItem == null || notification.ScheduleId == null)
                return null;

            var messageText = $"🔔 Напоминание: Занятие '{scheduleItem.Subject.Name}' начинается в {scheduleItem.StartTime:hh\\:mm}{Environment.NewLine}";
            if (!string.IsNullOrEmpty(scheduleItem.Building))
                messageText += $"Здание - {scheduleItem.Building}. ";
            if (!string.IsNullOrEmpty(scheduleItem.Room))
                messageText += $"Аудитория - {scheduleItem.Room}";
            messageText += $"{Environment.NewLine}Нажмите кнопку, чтобы подтвердить присутствие.";

            // Кнопка отметки. Используем Id предмета (Schedule.Id)
            var markup = BotKeyboard.InlineRow(
                new BotButton("Я на паре", $"attend_{notification.StudentId}_{notification.ScheduleId}"));

            return (messageText, markup);
        }

        private async Task<(string Text, BotKeyboard? Keyboard)?> BuildEndDayReportAsync(Notification notification)
        {
            // Отчёт собираем за день уведомления, а не за «сегодня»: пролежавшее до следующих
            // суток уведомление иначе отрисуется пустым
            var reportDate = DateOnly.FromDateTime(notification.ObjectCreateDate);

            // Берём отметки вместе с архивными: староста мог перезалить расписание или список
            // группы между созданием уведомления и его отправкой, и тогда отчёт ушёл бы пустым
            var attendanceData = await _attendanceRepository.GetAttendanceForReportAsync(reportDate);

            return (LeaderListFormatter.FormatGroupList(attendanceData, reportDate), null);
        }
    }
}
