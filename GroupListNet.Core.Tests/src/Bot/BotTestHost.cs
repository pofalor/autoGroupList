using GroupListNet.Core.src.Bot;
using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.DataAccess;
using GroupListNet.Core.src.DataAccess.BaseClasses;
using GroupListNet.Core.src.DataAccess.Repositories;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using GroupListNet.Core.src.Services;
using GroupListNet.Core.src.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GroupListNet.Core.Tests.src.Bot
{
    /// <summary>
    /// Поднимает обработчик бота на базе в памяти: команды, репозитории и права старосты настоящие,
    /// подменены только транспорты мессенджеров
    /// </summary>
    public class BotTestHost : IDisposable
    {
        private readonly ServiceProvider _provider;

        public BotTestHost(string? deadlineTime = null,
            string leaderTelegramIds = "",
            string leaderVkIds = "",
            string adminTelegramIds = "")
        {
            var settings = new Dictionary<string, string?>
            {
                ["SendingSettings:DeadlineTime"] = deadlineTime ?? "23:59",
                ["TelegramSettings:TelegramBotToken"] = "test-token",
                ["TelegramSettings:LeaderTelegramIds"] = leaderTelegramIds,
                ["TelegramSettings:AdminTelegramIds"] = adminTelegramIds,
                ["VkSettings:VkGroupToken"] = string.Empty,
                ["VkSettings:LeaderVkIds"] = leaderVkIds
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var databaseName = Guid.NewGuid().ToString();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddDbContext<ApplicationDbContext>(options => options
                .UseInMemoryDatabase(databaseName)
                .AddInterceptors(new RowVersionInterceptor()));
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<IGroupSettingsRepository, GroupSettingsRepository>();
            services.AddScoped<ILeaderRepository, LeaderRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IScheduleRepository, ScheduleRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<ILeaderService, LeaderService>();
            services.AddScoped<ILogNotificatorService, LogNotificatorService>();
            services.AddScoped<NotificationMessageBuilder>();
            services.AddSingleton<IMessengerClient>(Telegram);
            services.AddSingleton<IMessengerClient>(Vk);
            services.AddSingleton<BotUpdateHandler>();

            _provider = services.BuildServiceProvider();
            Handler = _provider.GetRequiredService<BotUpdateHandler>();
        }

        public BotUpdateHandler Handler { get; }

        public RecordingMessengerClient Telegram { get; } = new(MessengerType.Telegram);

        public RecordingMessengerClient Vk { get; } = new(MessengerType.Vk);

        public RecordingMessengerClient ClientOf(MessengerType messenger)
        {
            return messenger == MessengerType.Telegram ? Telegram : Vk;
        }

        /// <summary>
        /// Отправляет боту текст от лица пользователя мессенджера
        /// </summary>
        public Task SendTextAsync(MessengerType messenger, string userId, string text)
        {
            var client = ClientOf(messenger);
            return Handler.HandleMessageAsync(client, new BotMessage
            {
                Messenger = messenger,
                UserId = userId,
                ChatId = userId,
                Text = text
            });
        }

        /// <summary>
        /// Имитирует нажатие кнопки под сообщением
        /// </summary>
        public Task PressButtonAsync(MessengerType messenger, string userId, string data, string messageText = "")
        {
            var client = ClientOf(messenger);
            return Handler.HandleCallbackAsync(client, new BotCallback
            {
                Messenger = messenger,
                UserId = userId,
                ChatId = userId,
                Data = data,
                CallbackId = "1",
                MessageId = "1",
                MessageText = messageText
            });
        }

        public async Task<Student> AddStudentAsync(int numberInGroup, string surName = "Иванов")
        {
            using var scope = _provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var student = new Student
            {
                Name = "Иван",
                FatherName = "Иванович",
                SurName = surName,
                NumberInGroup = numberInGroup
            };
            context.Set<Student>().Add(student);
            await context.SaveChangesAsync();
            return student;
        }

        public async Task<Schedule> AddTodayScheduleAsync(string subjectName = "Матанализ",
            string building = "",
            string room = "")
        {
            using var scope = _provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subject = new Subject { Name = subjectName };
            context.Set<Subject>().Add(subject);
            await context.SaveChangesAsync();

            var schedule = new Schedule
            {
                SubjectId = subject.Id,
                // Конкретная дата вместо дня недели: занятие точно попадает на сегодня
                Date = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeSpan(9, 0, 0),
                ClassType = ClassType.Lecture,
                Building = building,
                Room = room
            };
            context.Set<Schedule>().Add(schedule);
            await context.SaveChangesAsync();
            return schedule;
        }

        /// <summary>
        /// Заводит уведомление так же, как это делают фоновые задачи, и при необходимости
        /// сдвигает дату создания в прошлое
        /// </summary>
        public async Task<int> AddNotificationAsync(NotificationType type,
            int studentId,
            int? scheduleId = null,
            MessengerType messenger = MessengerType.Telegram,
            DateOnly? createdOn = null)
        {
            using var scope = _provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            if (type == NotificationType.StartClass)
                await repository.RecordStartClassNotificationAsync(studentId, scheduleId!.Value, messenger);
            else
                await repository.RecordDailyReportSentAsync(studentId, createdOn ?? DateOnly.FromDateTime(DateTime.UtcNow), messenger);

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notification = await context.Set<Notification>()
                .OrderByDescending(item => item.Id)
                .FirstAsync();

            if (createdOn != null)
            {
                // Дату создания проставляет контекст, поэтому сдвигаем её отдельно
                notification.ObjectCreateDate = createdOn.Value.ToDateTime(new TimeOnly(18, 0));
                await context.SaveChangesAsync();
            }

            return notification.Id;
        }

        /// <summary>
        /// Собирает сообщение так же, как это делает отправщик: уведомление берётся тем же
        /// запросом, поэтому проверяются и его Include
        /// </summary>
        public async Task<(string Text, BotKeyboard? Keyboard)?> BuildMessageAsync(int notificationId,
            MessengerType messenger = MessengerType.Telegram)
        {
            using var scope = _provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var builder = scope.ServiceProvider.GetRequiredService<NotificationMessageBuilder>();

            var notification = (await repository.GetUnsentNotificationsAsync(messenger))
                .Single(item => item.Id == notificationId);

            return await builder.BuildAsync(notification);
        }

        /// <summary>
        /// Делает из уведомления несогласованную запись: тип «начало пары», а расписания нет.
        /// Штатно такого не бывает, нужно для проверки защитной ветки сборщика
        /// </summary>
        public async Task ChangeNotificationTypeToStartClassAsync(int notificationId)
        {
            using var scope = _provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notification = await context.Set<Notification>().FirstAsync(item => item.Id == notificationId);
            notification.NotificationType = NotificationType.StartClass;
            await context.SaveChangesAsync();
        }

        public async Task MarkAttendanceAsync(int studentId, int scheduleId, DateTime date)
        {
            using var scope = _provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
            await repository.MarkAttendanceAsync(studentId, scheduleId, date);
        }

        /// <summary>
        /// Помечает удалённым всё, что уходит в архив при перезаливке расписания и списка группы
        /// </summary>
        public async Task ArchiveGroupDataAsync()
        {
            using var scope = _provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var attendance in await context.Set<Attendance>().ToListAsync())
                attendance.IsDeleted = true;
            foreach (var schedule in await context.Set<Schedule>().ToListAsync())
                schedule.IsDeleted = true;
            foreach (var subject in await context.Set<Subject>().ToListAsync())
                subject.IsDeleted = true;
            foreach (var student in await context.Set<Student>().ToListAsync())
                student.IsDeleted = true;

            await context.SaveChangesAsync();
        }

        public async Task<Student?> ReloadStudentAsync(int studentId)
        {
            using var scope = _provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Set<Student>().FirstOrDefaultAsync(student => student.Id == studentId);
        }

        public async Task<int> CountAttendanceAsync(int studentId, int scheduleId)
        {
            using var scope = _provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.Set<Attendance>()
                .CountAsync(attendance => attendance.StudentId == studentId
                    && attendance.ScheduleId == scheduleId
                    && !attendance.IsDeleted);
        }

        public void Dispose()
        {
            _provider.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
