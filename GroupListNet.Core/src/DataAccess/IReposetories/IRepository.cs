using GroupListNet.Core.src.Entities;
using System.Linq.Expressions;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface IRepository<T> where T : PersistentEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task SaveChangesAsync();
    }
}
