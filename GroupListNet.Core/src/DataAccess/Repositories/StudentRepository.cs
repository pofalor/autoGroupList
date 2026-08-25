using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using Microsoft.EntityFrameworkCore;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class StudentRepository(ApplicationDbContext context) : Repository<Student>(context), IStudentRepository
    {
        public async Task<Student?> GetByMessengerIdAsync(MessengerType messenger, string messengerId)
        {
            if (string.IsNullOrWhiteSpace(messengerId))
                return null;

            return messenger switch
            {
                MessengerType.Telegram => await _dbSet.FirstOrDefaultAsync(s => s.TelegramId == messengerId && !s.IsDeleted),
                MessengerType.Vk => await _dbSet.FirstOrDefaultAsync(s => s.VkId == messengerId && !s.IsDeleted),
                _ => throw new ArgumentOutOfRangeException(nameof(messenger), messenger, "Неизвестный мессенджер.")
            };
        }

        public async Task<Student?> GetByNumberInGroupAsync(int number)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.NumberInGroup == number && !s.IsDeleted);
        }

        public async Task<bool> IsNumberTakenAsync(int number, MessengerType messenger)
        {
            return messenger switch
            {
                MessengerType.Telegram => await _dbSet.AnyAsync(s => s.NumberInGroup == number && s.TelegramId != null && !s.IsDeleted),
                MessengerType.Vk => await _dbSet.AnyAsync(s => s.NumberInGroup == number && s.VkId != null && !s.IsDeleted),
                _ => throw new ArgumentOutOfRangeException(nameof(messenger), messenger, "Неизвестный мессенджер.")
            };
        }

        public async Task<IEnumerable<Student>> GetStudentsWithAnyMessengerAsync()
        {
            return await _dbSet.Where(s => (s.TelegramId != null || s.VkId != null) && !s.IsDeleted)
                             .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsWithoutAccountAsync(MessengerType messenger)
        {
            return messenger switch
            {
                MessengerType.Telegram => await _dbSet.Where(s => s.TelegramId == null && !s.IsDeleted).ToListAsync(),
                MessengerType.Vk => await _dbSet.Where(s => s.VkId == null && !s.IsDeleted).ToListAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(messenger), messenger, "Неизвестный мессенджер.")
            };
        }

        public async Task UpdateStudentSubgroupAsync(MessengerType messenger, string messengerId, Subgroup? subgroup)
        {
            var student = await GetByMessengerIdAsync(messenger, messengerId);
            if (student != null)
            {
                student.Subgroup = subgroup;
                await UpdateAsync(student);
                await SaveChangesAsync();
            }
        }

        public async Task ResetSubgroupsAsync()
        {
            var studentsInSubgroups = await _dbSet.Where(s => !s.IsDeleted && s.Subgroup != null).ToListAsync();
            foreach (var student in studentsInSubgroups)
            {
                student.Subgroup = null;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Student>> DeleteAllAsync()
        {
            var allStudents = await _dbSet.Where(s => !s.IsDeleted).ToListAsync();
            foreach (var item in allStudents)
            {
                item.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
            return allStudents;
        }
    }
}
