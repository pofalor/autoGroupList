using Microsoft.AspNetCore.Mvc;
using GroupListNet.Web.Api.Attributes;

namespace GroupListNet.Controllers.BaseControllers
{
    /// <summary>
    /// Базовый контроллер
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiRequestValidation]
    public abstract class BaseApiController : ControllerBase
    {
    }
}
