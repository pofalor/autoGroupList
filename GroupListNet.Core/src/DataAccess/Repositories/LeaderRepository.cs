using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class LeaderRepository : Repository<Leader>, ILeaderRepository
    {
        public LeaderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<bool> IsLeaderAsync(string telegramId)
        {
            return await _dbSet.Include(l => l.Student)
                             .AnyAsync(l => l.Student.TelegramId == telegramId &&
                                            !l.Student.IsDeleted &&
                                            !l.IsDeleted);
        }

        public async Task<int[]> GetLeaderIds()
        {
            var leaderIds = await _dbSet.Include(l => l.Student)
                                   .Where(l => !l.IsDeleted)
                                   .Where(l => !l.Student.IsDeleted)
                                   .Select(x => x.Student.Id)
                                   .ToArrayAsync();

            return leaderIds;
        }

        public async Task<Student[]> GetLeaderStudentsAsync()
        {
            return await _dbSet.Include(l => l.Student)
                .Where(l => !l.IsDeleted)
                .Where(l => !l.Student.IsDeleted)
                .Select(l => l.Student)
                .ToArrayAsync();
        }

        public async Task DeleteLeaderByStudentIdsAsync(IEnumerable<int> studentIds)
        {
            var leaders = await _dbSet.Where(l => !l.IsDeleted && studentIds.Contains(l.StudentId))
                .ToListAsync();

            foreach (var leader in leaders)
            {
                leader.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
