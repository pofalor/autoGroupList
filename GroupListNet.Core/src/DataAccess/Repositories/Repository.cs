using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class Repository<T> : IRepository<T> where T : PersistentEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(x => !x.IsDeleted).Where(predicate).ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await Task.CompletedTask;
        }

        public virtual async Task DeleteAsync(T entity)
        {
            entity.IsDeleted = true;
            await UpdateAsync(entity);
        }

        public virtual async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
