using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.NetworkInformation;
using GroupListNet.Core.src.BackgroundJobs;
using GroupListNet.Core.src.Services;
using GroupListNet.Core.src.Services.Impl;
using GroupListNet.Core.src.DataAccess.BaseClasses;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.DataAccess.Repositories;
using GroupListNet.Addons;
using GroupListNet.Core.src.Bot;
using GroupListNet.Core.src.Bot.Clients;

namespace GroupListNet.Core.src.Installers
{
    public static class CoreInstaller
    {
        public static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.AddCoreServices();
            services.AddMessengerClients();
            services.AddBackgroundJobs();
            services.AddRepositories();
            services.AddMarker(); // Приватный пакет переноса отметок на сайт
            return services;
        }

        /// <summary>
        /// Установить сервисы
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddScoped<ILeaderService, LeaderService>();
            services.AddScoped<ILogNotificatorService, LogNotificatorService>();
            services.AddScoped<ISosService, SosService>();

            return services;
        }

        /// <summary>
        /// Транспорты бота и общий обработчик команд. Клиент каждого мессенджера регистрируем и сам по себе,
        /// потому что его фоновой задаче нужен доступ к клиенту библиотеки
        /// </summary>
        public static IServiceCollection AddMessengerClients(this IServiceCollection services)
        {
            services.AddSingleton<TelegramMessengerClient>();
            services.AddSingleton<VkMessengerClient>();
            services.AddSingleton<IMessengerClient>(provider => provider.GetRequiredService<TelegramMessengerClient>());
            services.AddSingleton<IMessengerClient>(provider => provider.GetRequiredService<VkMessengerClient>());
            services.AddSingleton<BotUpdateHandler>();

            return services;
        }

        public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
        {
            services.AddHostedService<LeaderBackgroundJob>();
            services.AddHostedService<TelegramBotBackgroundJob>();
            services.AddHostedService<VkBotBackgroundJob>();
            services.AddHostedService<NotificationSenderBackgroundJob>();
            services.AddHostedService<StudentScheduleBackgroundJob>();
            services.AddHostedService<AttendanceSyncBackgroundJob>();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<IGroupSettingsRepository, GroupSettingsRepository>();
            services.AddScoped<ILeaderRepository, LeaderRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IScheduleRepository, ScheduleRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();

            return services;
        }
    }
}
