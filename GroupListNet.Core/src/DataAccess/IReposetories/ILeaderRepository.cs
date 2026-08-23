using GroupListNet.Core.src.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface ILeaderRepository : IRepository<Leader>
    {
        Task<bool> IsLeaderAsync(string telegramId);
        Task<int[]> GetLeaderIds();
        Task<Student[]> GetLeaderStudentsAsync();
        Task DeleteLeaderByStudentIdsAsync(IEnumerable<int> studentIds);
    }
}
