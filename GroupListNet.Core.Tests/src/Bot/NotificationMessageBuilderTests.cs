using GroupListNet.Core.src.Entities.Utils;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.Tests.src.Bot
{
    /// <summary>
    /// Текст уведомления в базе не хранится, поэтому проверяем, что он целиком собирается
    /// из расписания и отметок — в том числе после перезаливки, когда данные ушли в архив
    /// </summary>
    public class NotificationMessageBuilderTests
    {
        [Fact]
        public async Task StartClass_BuildsReminderWithAttendButton()
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(1);
            var schedule = await host.AddTodayScheduleAsync("Матанализ");
            var notificationId = await host.AddNotificationAsync(NotificationType.StartClass, student.Id, schedule.Id);

            var message = await host.BuildMessageAsync(notificationId);

            Assert.NotNull(message);
            Assert.Contains("Матанализ", message.Value.Text);
            Assert.Contains("09:00", message.Value.Text);

            Assert.NotNull(message.Value.Keyboard);
            var button = Assert.Single(Assert.Single(message.Value.Keyboard.Rows));
            Assert.Equal("Я на паре", button.Text);
            Assert.Equal($"attend_{student.Id}_{schedule.Id}", button.Data);
        }

        [Fact]
        public async Task StartClass_WithBuildingAndRoom_ShowsThem()
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(1);
            var schedule = await host.AddTodayScheduleAsync("Физика", building: "2", room: "301");
            var notificationId = await host.AddNotificationAsync(NotificationType.StartClass, student.Id, schedule.Id);

            var message = await host.BuildMessageAsync(notificationId);

            Assert.NotNull(message);
            Assert.Contains("Здание - 2", message.Value.Text);
            Assert.Contains("Аудитория - 301", message.Value.Text);
        }

        [Fact]
        public async Task StartClass_ArchivedSchedule_StillBuildsText()
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(1);
            var schedule = await host.AddTodayScheduleAsync("Матанализ");
            var notificationId = await host.AddNotificationAsync(NotificationType.StartClass, student.Id, schedule.Id);

            // Расписание уехало в архив, но уведомление уже успело попасть в отправку
            await host.ArchiveGroupDataAsync();
            var message = await host.BuildMessageAsync(notificationId);

            Assert.NotNull(message);
            Assert.Contains("Матанализ", message.Value.Text);
        }

        [Fact]
        public async Task StartClass_WithoutSchedule_ReturnsNull()
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(1);
            // Уведомление о паре без расписания в жизни не создаётся, это защита от битых данных
            var notificationId = await host.AddNotificationAsync(NotificationType.EndDayReport, student.Id);
            await host.ChangeNotificationTypeToStartClassAsync(notificationId);

            var message = await host.BuildMessageAsync(notificationId);

            Assert.Null(message);
        }

        [Fact]
        public async Task EndDayReport_ListsEveryoneWhoMarked()
        {
            using var host = new BotTestHost();
            var leader = await host.AddStudentAsync(1, "Иванов");
            var otherStudent = await host.AddStudentAsync(2, "Петров");
            var schedule = await host.AddTodayScheduleAsync("Матанализ");
            await host.MarkAttendanceAsync(leader.Id, schedule.Id, DateTime.UtcNow);
            await host.MarkAttendanceAsync(otherStudent.Id, schedule.Id, DateTime.UtcNow);

            var notificationId = await host.AddNotificationAsync(NotificationType.EndDayReport, leader.Id);
            var message = await host.BuildMessageAsync(notificationId);

            Assert.NotNull(message);
            Assert.Contains("Иванов", message.Value.Text);
            Assert.Contains("Петров", message.Value.Text);
            Assert.Contains("Матанализ", message.Value.Text);
            // У отчёта кнопок нет
            Assert.Null(message.Value.Keyboard);
        }

        [Fact]
        public async Task EndDayReport_AfterGroupDataArchived_StillListsEveryone()
        {
            using var host = new BotTestHost();
            var leader = await host.AddStudentAsync(1, "Иванов");
            var schedule = await host.AddTodayScheduleAsync("Матанализ");
            await host.MarkAttendanceAsync(leader.Id, schedule.Id, DateTime.UtcNow);
            var notificationId = await host.AddNotificationAsync(NotificationType.EndDayReport, leader.Id);

            // Староста перезалил расписание или список группы между созданием отчёта и его отправкой
            await host.ArchiveGroupDataAsync();
            var message = await host.BuildMessageAsync(notificationId);

            Assert.NotNull(message);
            Assert.Contains("Иванов", message.Value.Text);
            Assert.Contains("Матанализ", message.Value.Text);
            Assert.DoesNotContain("никто не отметился", message.Value.Text);
        }

        [Fact]
        public async Task EndDayReport_TakesAttendanceOfItsOwnDate()
        {
            using var host = new BotTestHost();
            var leader = await host.AddStudentAsync(1, "Иванов");
            var schedule = await host.AddTodayScheduleAsync("Матанализ");
            // Отметка сделана сегодня, а отчёт — за вчера
            await host.MarkAttendanceAsync(leader.Id, schedule.Id, DateTime.UtcNow);

            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            var notificationId = await host.AddNotificationAsync(NotificationType.EndDayReport, leader.Id, createdOn: yesterday);

            var message = await host.BuildMessageAsync(notificationId);

            Assert.NotNull(message);
            Assert.Equal(LeaderListFormatter.FormatGroupList([], yesterday), message.Value.Text);
            Assert.DoesNotContain("Иванов", message.Value.Text);
        }
    }
}
