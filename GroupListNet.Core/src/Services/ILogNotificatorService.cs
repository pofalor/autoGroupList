using Microsoft.Extensions.Configuration;
using GroupListNet.Core.src.DataResult;

namespace GroupListNet.Core.src.Services
{
    public interface ILogNotificatorService
    {
        Task<IDataResult<bool>> SendTelegramAdminAsync(string text);

        Task<IDataResult<bool>> LogAndNotifyAdminsAsync(string text, Exception? exception = null);
    }
}
