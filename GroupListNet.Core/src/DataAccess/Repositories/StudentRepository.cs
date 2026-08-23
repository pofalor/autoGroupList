using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using Microsoft.EntityFrameworkCore;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class StudentRepository(ApplicationDbContext context) : Repository<Student>(context), IStudentRepository
    {
        public async Task<Student?> GetByTelegramIdAsync(string telegramId)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.TelegramId == telegramId && !s.IsDeleted);
        }

        public async Task<Student?> GetByNumberInGroupAsync(int number)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.NumberInGroup == number && !s.IsDeleted);
        }

        public async Task<bool> IsNumberTakenAsync(int number)
        {
            return await _dbSet.AnyAsync(s => s.NumberInGroup == number &&
                                            s.TelegramId != null &&
                                            !s.IsDeleted);
        }

        public async Task<IEnumerable<Student>> GetStudentsWithTelegramAsync()
        {
            return await _dbSet.Where(s => s.TelegramId != null && !s.IsDeleted)
                             .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsWithoutTelegramAsync()
        {
            return await _dbSet.Where(s => s.TelegramId == null && !s.IsDeleted)
                             .ToListAsync();
        }

        public async Task UpdateStudentSubgroupAsync(string telegramId, Subgroup? subgroup)
        {
            var student = await GetByTelegramIdAsync(telegramId);
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
