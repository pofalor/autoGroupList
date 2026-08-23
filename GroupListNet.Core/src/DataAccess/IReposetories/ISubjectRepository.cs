using GroupListNet.Core.src.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface ISubjectRepository : IRepository<Subject>
    {
        Task DeleteAllAsync();

        Task<Subject?> GetByNameAsync(string name);
    }
}
