using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using GroupListNet.Utils.src.Extensions;
using Microsoft.EntityFrameworkCore;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class ScheduleRepository(ApplicationDbContext context) : Repository<Schedule>(context), IScheduleRepository
    {
        public async Task<IEnumerable<Schedule>> GetScheduleForStudentAsync(Subgroup? subgroup, WeekType? weekType, DayOfWeek dayOfWeek)
        {
            var query = _dbSet.Where(s => !s.IsDeleted &&
                                        s.DayOfWeek == dayOfWeek &&
                                        (s.WeekType == weekType || s.WeekType == null) &&
                                        (s.Subgroup == subgroup || s.Subgroup == null));

            return await query.Include(s => s.Subject)
                            .OrderBy(s => s.StartTime)
                            .ToListAsync();
        }

        public async Task<Schedule?> GetWithSubjectAsync(int id)
        {
           return await _dbSet.Include(x=> x.Subject).Where(x=> x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Schedule>> GetTodayScheduleAsync(Subgroup? subgroup, WeekType weekType, DayOfWeek dayOfWeek)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = _dbSet
                .Where(s => !s.IsDeleted &&
                                        ((s.DayOfWeek == dayOfWeek &&
                                          (s.WeekType == weekType || s.WeekType == null) &&
                                          (s.Subgroup == subgroup || s.Subgroup == null)) ||
                                         (s.Date == today &&
                                          (s.Subgroup == subgroup || s.Subgroup == null))));

            return await query.Include(s => s.Subject)
                            .OrderBy(s => s.StartTime)
                            .ToListAsync();
        }

        public async Task<IEnumerable<Schedule>> GetStartedClassesAsync(WeekType weekType, DayOfWeek dayOfWeek, TimeSpan startTime)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = _dbSet
                .Where(s => !s.IsDeleted)
                .Where(s => (s.DayOfWeek == dayOfWeek && 
                            (s.WeekType == weekType || s.WeekType == null)) ||
                            (s.Date == today))
                //Берём занятия, которые уже начались
                .Where(s=> s.StartTime <= startTime);

            return await query.Include(s => s.Subject)
                            .OrderBy(s => s.StartTime)
                            .ToListAsync();
        }

        public async Task<IEnumerable<int>> DeleteAllAsync()
        {
            // Удаление всех записей в таблице 
            var allSchedules = await _dbSet.Where(s => !s.IsDeleted).ToListAsync();
            foreach (var item in allSchedules)
            {
                item.IsDeleted = true;
            }
            
            await _context.SaveChangesAsync();
            return allSchedules.Select(x => x.Id);
        }
    }
}
