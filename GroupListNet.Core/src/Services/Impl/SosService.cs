using Microsoft.Extensions.Logging;
using GroupListNet.Core.src.DataAccess;

namespace GroupListNet.Core.src.Services.Impl
{
    public class SosService : ISosService
    {
        readonly ILogger<SosService> _logger;
        readonly ApplicationDbContext _dbContext;

        public SosService(ILogger<SosService> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }
    }
}
