using Microsoft.AspNetCore.Mvc;
using GroupListNet.Controllers.BaseControllers;
using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.Services;

namespace GroupListNet.Web.Api.Controllers
{
    [Route("api/sos")]
    [ApiController]
    public class SosController : BaseApiController
    {
        private readonly ILogger<SosController> _logger;
        private readonly ISosService _sosService;
        readonly string AnonymousTokenRequest;
        public SosController(ILogger<SosController> logger, ISosService sosService, IConfiguration config)
        {
            _logger = logger;
            _sosService = sosService;

            try
            {
                AnonymousTokenRequest = config
                    .GetSection(SecurityConfiguration.SecuritySectionInConfig)
                    .Get<SecurityConfiguration>()?.AnonymousTokenRequest
                    ?? throw new InvalidOperationException($"Cannot get {SecurityConfiguration.SecuritySectionInConfig} section, " +
                        $"{nameof(SecurityConfiguration.AnonymousTokenRequest)} key from config. " +
                        $"Value is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} fatal error getting value from config!{NewLine}",
                    nameof(SosController), Environment.NewLine);
                throw;
            }
        }
    }
}
