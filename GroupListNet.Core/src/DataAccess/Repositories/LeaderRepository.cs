using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using Microsoft.EntityFrameworkCore;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class LeaderRepository : Repository<Leader>, ILeaderRepository
    {
        public LeaderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<bool> IsLeaderAsync(MessengerType messenger, string messengerId)
        {
            if (string.IsNullOrWhiteSpace(messengerId))
                return false;

            var leaders = _dbSet.Include(l => l.Student)
                .Where(l => !l.Student.IsDeleted && !l.IsDeleted);

            return messenger switch
            {
                MessengerType.Telegram => await leaders.AnyAsync(l => l.Student.TelegramId == messengerId),
                MessengerType.Vk => await leaders.AnyAsync(l => l.Student.VkId == messengerId),
                _ => throw new ArgumentOutOfRangeException(nameof(messenger), messenger, "Неизвестный мессенджер.")
            };
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
