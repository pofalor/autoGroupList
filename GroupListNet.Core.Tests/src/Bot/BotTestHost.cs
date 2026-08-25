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

        public async Task<Schedule> AddTodayScheduleAsync(string subjectName = "Матанализ")
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
                ClassType = ClassType.Lecture
            };
            context.Set<Schedule>().Add(schedule);
            await context.SaveChangesAsync();
            return schedule;
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
