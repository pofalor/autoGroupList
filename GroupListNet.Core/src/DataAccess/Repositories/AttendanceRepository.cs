using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(ApplicationDbContext context) : base(context) { }

        public async Task MarkAttendanceAsync(int studentId, int scheduleId, DateTime date)
        {
            var today = DateOnly.FromDateTime(date);
            var existingAttendance = await _dbSet
                .FirstOrDefaultAsync(a => a.StudentId == studentId &&
                                        a.ScheduleId == scheduleId &&
                                        DateOnly.FromDateTime(a.Date) == today &&
                                        !a.IsDeleted);

            if (existingAttendance != null)
            {
                existingAttendance.Date = date;
                await UpdateAsync(existingAttendance);
                await SaveChangesAsync();
            }
            else
            {
                var attendance = new Attendance
                {
                    StudentId = studentId,
                    ScheduleId = scheduleId,
                    Date = date
                };
                await AddAsync(attendance);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Attendance>> GetTodayAttendanceAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            return await _dbSet.Where(a => DateOnly.FromDateTime(a.Date) == today && !a.IsDeleted)
                             .Include(a => a.Student)
                             .Include(a => a.Schedule)
                             .ThenInclude(s => s!.Subject)
                             .OrderBy(a => a.Schedule!.StartTime)
                             .ThenBy(a => a.Student!.NumberInGroup)
                             .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAttendanceForReportAsync(DateOnly date)
        {
            // Условия на IsDeleted нет намеренно: перезаливка расписания или списка группы
            // помечает отметки удалёнными, но в отчёт за прошедший день они должны попасть
            return await _dbSet.Where(a => DateOnly.FromDateTime(a.Date) == date)
                             .Include(a => a.Student)
                             .Include(a => a.Schedule)
                             .ThenInclude(s => s!.Subject)
                             .OrderBy(a => a.Schedule!.StartTime)
                             .ThenBy(a => a.Student!.NumberInGroup)
                             .ToListAsync();
        }

        public async Task<bool> HasAttendanceAsync(int studentId, int scheduleId, DateOnly date)
        {
            return await _dbSet.AnyAsync(a => a.StudentId == studentId &&
                                            a.ScheduleId == scheduleId &&
                                            DateOnly.FromDateTime(a.Date) == date &&
                                            !a.IsDeleted);
        }

        public async Task DeleteAttendanceByScheduleIdsAsync(IEnumerable<int> scheduleIds)
        {
            // Удаление всех записей в таблице 
            var allAttendance = await _dbSet.Where(s => !s.IsDeleted && scheduleIds.Contains(s.ScheduleId)).ToListAsync();
            foreach (var item in allAttendance)
            {
                item.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAttendanceByStudentIdsAsync(IEnumerable<int> studentIds)
        {
            var allAttendance = await _dbSet.Where(s => !s.IsDeleted && studentIds.Contains(s.StudentId)).ToListAsync();
            foreach (var item in allAttendance)
            {
                item.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
