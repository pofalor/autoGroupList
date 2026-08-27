using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.DataResult;

namespace GroupListNet.Core.src.Services.Impl
{
    /// <summary>
    /// Технические уведомления администраторам. Ходит в API мессенджеров напрямую,
    /// чтобы не зависеть от бот-клиентов: сервис зовут в том числе из их конструкторов
    /// </summary>
    public class LogNotificatorService : ILogNotificatorService
    {
        private const string VkApiVersion = "5.199";

        private readonly ILogger<LogNotificatorService> _logger;

        private readonly TelegramSettingsConfiguration TelegramConfig;

        private readonly VkSettingsConfiguration VkConfig;

        public LogNotificatorService(ILogger<LogNotificatorService> logger, IConfiguration config)
        {
            _logger = logger;

            try
            {
                TelegramConfig = config.GetSection(TelegramSettingsConfiguration.TelegramSectionInConfig).Get<TelegramSettingsConfiguration>()
                ?? throw new InvalidOperationException($"Cannot get {TelegramSettingsConfiguration.TelegramSectionInConfig} section from config. " +
                $"Value is null.");

                // ВКонтакте необязателен, поэтому пустую секцию считаем выключенным мессенджером
                VkConfig = config.GetSection(VkSettingsConfiguration.VkSectionInConfig).Get<VkSettingsConfiguration>()
                    ?? new VkSettingsConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} fatal error getting value from config!{NewLine}",
                    nameof(LogNotificatorService), Environment.NewLine);
                throw;
            }
        }

        public async Task<IDataResult<bool>> NotifyAdminsAsync(string text)
        {
            var result = new DataResult<bool>();

            var resp = (await SendTelegramAdminAsync(text)).Success;
            resp &= (await SendVkAdminAsync(text)).Success;

            return result.WithData(resp);
        }

        public async Task<IDataResult<bool>> LogAndNotifyAdminsAsync(string text, Exception? exception = null)
        {
            var result = new DataResult<bool>();
            bool resp = true;
            try
            {
                _logger.LogError(exception, text);
                resp &= (await NotifyAdminsAsync(text)).Success;
            }
            catch (Exception ex)
            {
                return result.WithError(ex.Message);
            }
            return result.WithData(resp);
        }

        private async Task<IDataResult<bool>> SendTelegramAdminAsync(string text)
        {
            var result = new DataResult<bool>();

            if (string.IsNullOrWhiteSpace(TelegramConfig.TelegramBotToken))
                return result.WithData(true);

            bool resp = true;
            foreach (var adminId in TelegramConfig.AdminTelegramIdsArray)
            {
                try
                {
                    resp &= (await SendBotTelegramAsync(text, adminId, TelegramConfig.TelegramBotToken)).Success;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Cannot send message to tg admin => [{Parameter} = {AdminId}]{NewLine}",
                        nameof(adminId), adminId, Environment.NewLine);
                }
            }
            return result.WithData(resp);
        }

        private async Task<IDataResult<bool>> SendVkAdminAsync(string text)
        {
            var result = new DataResult<bool>();

            // Без токена сообщества ВКонтакте выключен, уведомлять некого
            if (!VkConfig.IsConfigured)
                return result.WithData(true);

            bool resp = true;
            foreach (var adminId in VkConfig.AdminVkIdsArray)
            {
                try
                {
                    resp &= (await SendBotVkAsync(text, adminId, VkConfig.VkGroupToken)).Success;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Cannot send message to vk admin => [{Parameter} = {AdminId}]{NewLine}",
                        nameof(adminId), adminId, Environment.NewLine);
                }
            }
            return result.WithData(resp);
        }

        private async Task<IDataResult<bool>> SendBotTelegramAsync(string text,
            string tgId,
            string botAddress)
        {
            var result = new DataResult<bool>();
            try
            {
                var url = string.Format(
                    "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}",
                    botAddress, tgId, Uri.EscapeDataString(text));


                using (var req = new HttpClient())
                {
                    var res = await req.GetAsync(url);
                    return result.WithData(res.IsSuccessStatusCode);
                }
            }
            catch (Exception ex)
            {
                string mes = $"Cannot send message to tg => [{nameof(tgId)} = {tgId}]";
                _logger.LogError(ex, mes);
                return result.WithError(mes);
            }
        }

        private async Task<IDataResult<bool>> SendBotVkAsync(string text,
            string vkId,
            string groupToken)
        {
            var result = new DataResult<bool>();
            try
            {
                // random_id обязателен: по нему ВКонтакте отсекает повторную отправку
                var url = string.Format(
                    "https://api.vk.com/method/messages.send?user_id={0}&message={1}&random_id={2}&access_token={3}&v={4}",
                    vkId, Uri.EscapeDataString(text), Random.Shared.NextInt64(), groupToken, VkApiVersion);

                using (var req = new HttpClient())
                {
                    var res = await req.GetAsync(url);
                    if (!res.IsSuccessStatusCode)
                        return result.WithData(false);

                    // ВКонтакте отвечает 200 и на ошибку, поэтому смотрим тело ответа
                    var body = await res.Content.ReadAsStringAsync();
                    return result.WithData(!body.Contains("\"error\""));
                }
            }
            catch (Exception ex)
            {
                string mes = $"Cannot send message to vk => [{nameof(vkId)} = {vkId}]";
                _logger.LogError(ex, mes);
                return result.WithError(mes);
            }
        }
    }
}
