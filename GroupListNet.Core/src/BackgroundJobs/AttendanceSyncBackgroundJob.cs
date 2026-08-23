using GroupListNet.Core.src.ConfigModels;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Addons;
using GroupListNet.Utils.src.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GroupListNet.Core.src.BackgroundJobs
{
    /// <summary>
    /// Фоновая задача переноса отметок на сайт. Сама работа с браузером вынесена в приватный
    /// NuGet-пакет <see cref="IMarker"/>: задача лишь собирает из БД, кого и на каких
    /// занятиях надо отметить, и передаёт эти данные вместе с учётными данными в пакет.
    /// </summary>
    public class AttendanceSyncBackgroundJob : BackgroundService
    {
        private readonly ILogger<AttendanceSyncBackgroundJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMarker _marker;

        private readonly string _siteBaseUrl;
        private readonly string _loginUrl;
        private readonly string _attendancePageUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly int _timeout;
        private readonly double _taskTimeout;
        private readonly bool _markAttendanceOnSite;
        private readonly string _chromeBinaryPath;
        private readonly string _chromeDriverDirectory;

        public AttendanceSyncBackgroundJob(
            ILogger<AttendanceSyncBackgroundJob> logger,
            IServiceScopeFactory scopeFactory,
            IMarker marker,
            IConfiguration config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _marker = marker;

            try
            {
                var siteSettings = config.GetSection(SiteSettings.SiteSettingsSectionInConfig).Get<SiteSettings>();
                _markAttendanceOnSite = siteSettings?.MarkAttendanceOnSite ?? false;
                _siteBaseUrl = siteSettings!.BaseUrl;
                _loginUrl = siteSettings.LoginUrl ?? _siteBaseUrl; // Если LoginUrl не задан, используем baseUrl
                _attendancePageUrl = siteSettings.AttendancePageUrl;
                _username = siteSettings.Username;
                _password = siteSettings.Password;
                _timeout = siteSettings.SiteTimeOut;
                _taskTimeout = siteSettings.TaskTimeoutMinutes;
                _chromeBinaryPath = siteSettings.ChromeBinaryPath;
                _chromeDriverDirectory = siteSettings.ChromeDriverDirectory;

                // Когда отметка на сайте выключена, логин с паролем не нужны
                if (_markAttendanceOnSite &&
                    (string.IsNullOrEmpty(_attendancePageUrl) ||
                     string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password)))
                {
                    throw new InvalidOperationException("Не все настройки сайта (AttendancePageUrl, Username, Password) заданы в конфигурации.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} error getting value from config!{NewLine}",
                    nameof(AttendanceSyncBackgroundJob), Environment.NewLine);
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_markAttendanceOnSite)
            {
                _logger.LogInformation("Отметка на сайте выключена настройкой {SectionName}:{SettingName}, " +
                    "{ClassName} не запускается.",
                    SiteSettings.SiteSettingsSectionInConfig, nameof(SiteSettings.MarkAttendanceOnSite), nameof(AttendanceSyncBackgroundJob));
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncAttendanceForTodayAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка во время синхронизации посещаемости.");
                }

                await Task.Delay(TimeSpan.FromMinutes(_taskTimeout), stoppingToken);
            }
        }

        private async Task SyncAttendanceForTodayAsync(CancellationToken cancellationToken)
        {
            // Собираем из БД отметки за сегодня
            var attendanceData = await GetAttendanceFromDatabaseAsync(cancellationToken);
            if (!attendanceData.Any())
            {
                _logger.LogInformation("На сегодня ({Today}) нет отмеченных посещений в БД для синхронизации.",
                    DateOnly.FromDateTime(DateTime.UtcNow));
                return;
            }

            // Превращаем отметки в список занятий для пакета: предмет + время + ФИО присутствующих
            var lessons = BuildLessons(attendanceData);
            if (lessons.Count == 0)
            {
                _logger.LogInformation("Не удалось сформировать ни одного занятия для отметки.");
                return;
            }

            var credentials = new Credentials(_username, _password);
            var options = new MarkerOptions
            {
                BaseUrl = _siteBaseUrl,
                LoginUrl = _loginUrl,
                PageUrl = _attendancePageUrl,
                ChromeBinaryPath = string.IsNullOrWhiteSpace(_chromeBinaryPath) ? null : _chromeBinaryPath,
                ChromeDriverDirectory = string.IsNullOrWhiteSpace(_chromeDriverDirectory) ? null : _chromeDriverDirectory,
                TimeoutSeconds = _timeout,
                Headless = true
            };

            var report = await _marker.MarkAsync(credentials, options, lessons, cancellationToken);
            LogReport(report);
        }

        private static List<LessonMarking> BuildLessons(IReadOnlyCollection<Attendance> attendanceData)
        {
            var lessons = new List<LessonMarking>();

            foreach (var group in attendanceData.GroupBy(a => a.ScheduleId))
            {
                var schedule = group.First().Schedule;
                if (schedule?.Subject == null)
                    continue;

                var fullNames = group
                    .Select(a => a.Student?.GetFullName())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .ToList();

                lessons.Add(new LessonMarking(schedule.Subject.Name, schedule.StartTime, fullNames));
            }

            return lessons;
        }

        private void LogReport(MarkingReport report)
        {
            if (!report.LoggedIn)
            {
                _logger.LogWarning("Не удалось авторизоваться на сайте — отметки не перенесены.");
                return;
            }

            foreach (var lesson in report.Lessons)
            {
                switch (lesson.Status)
                {
                    case LessonStatus.Marked:
                        _logger.LogInformation("Занятие '{Subject}' ({Time}): отмечено {Present}, снято {Absent}.",
                            lesson.SubjectName, lesson.StartTime, lesson.MarkedPresent, lesson.MarkedAbsent);
                        break;
                    case LessonStatus.NotFound:
                        _logger.LogWarning("Занятие '{Subject}' ({Time}) не найдено в расписании на сайте.",
                            lesson.SubjectName, lesson.StartTime);
                        break;
                    case LessonStatus.Skipped:
                        _logger.LogWarning("Занятие '{Subject}' ({Time}) пропущено: {Message}",
                            lesson.SubjectName, lesson.StartTime, lesson.Message);
                        break;
                    case LessonStatus.Error:
                        _logger.LogError("Занятие '{Subject}' ({Time}): ошибка — {Message}",
                            lesson.SubjectName, lesson.StartTime, lesson.Message);
                        break;
                }
            }

            _logger.LogInformation("Синхронизация посещаемости за {Today} завершена.",
                DateOnly.FromDateTime(DateTime.UtcNow));
        }

        private async Task<List<Attendance>> GetAttendanceFromDatabaseAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var attendanceRepository = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
            var studentRepository = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
            var scheduleRepository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();

            var attendanceRecords = await attendanceRepository.GetTodayAttendanceAsync();

            // Подстраховка: догружаем связанные сущности, если они не пришли вместе с отметкой
            foreach (var record in attendanceRecords)
            {
                if (record.Student == null)
                    record.Student = await studentRepository.GetByIdAsync(record.StudentId);
                if (record.Schedule == null)
                    record.Schedule = await scheduleRepository.GetWithSubjectAsync(record.ScheduleId);
            }

            return attendanceRecords.ToList();
        }
    }
}
