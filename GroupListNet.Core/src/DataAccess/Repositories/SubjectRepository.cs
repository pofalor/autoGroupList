using GroupListNet.Core.src.DataAccess.BaseClasses;
using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.DataAccess.Repositories
{
    public class SubjectRepository(ApplicationDbContext context) : Repository<Subject>(context), ISubjectRepository
    {
        public async Task DeleteAllAsync()
        {
            // Удаление всех записей в таблице 
            var allSubjects = await _dbSet.Where(s => !s.IsDeleted).ToListAsync();
            foreach (var item in allSubjects)
            {
                item.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Находит предмет по его имени
        /// </summary>
        public async Task<Subject?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower() && !s.IsDeleted);
        }
    }
}
