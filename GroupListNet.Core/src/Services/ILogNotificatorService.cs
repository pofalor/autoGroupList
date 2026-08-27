using GroupListNet.Core.src.DataResult;

namespace GroupListNet.Core.src.Services
{
    public interface ILogNotificatorService
    {
        /// <summary>
        /// Пишет администраторам во все настроенные мессенджеры
        /// </summary>
        Task<IDataResult<bool>> NotifyAdminsAsync(string text);

        Task<IDataResult<bool>> LogAndNotifyAdminsAsync(string text, Exception? exception = null);
    }
}
