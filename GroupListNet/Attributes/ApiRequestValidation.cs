using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using GroupListNet.Core.src.Enums.ErrorCodes;
using GroupListNet.Web.Api.Extensions;
using GroupListNet.Web.Api.Responses;

namespace GroupListNet.Web.Api.Attributes
{
    /// <summary>
    /// Фильтр, который делает стандартные проверки модели.
    /// В случае ошибки возвращает BaseApiResponse.
    /// Справедлив для методов апи post и put - где функция принимает один параметр-запрос
    /// </summary>
    public class ApiRequestValidationAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Вызывается перед выполнением метода контроллера
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Validate(context);
            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Вызывается перед выполнением метода контроллера
        /// </summary>
        private static void Validate(ActionExecutingContext actionContext)
        {
            var actionArguments = actionContext.ActionArguments;
            if (actionArguments.Count == 1)
            {
                var request = actionArguments.First();
                if (request.Value == null)
                {
                    actionContext.ModelState.AddModelError("", "Empty request");
                }
            }

            if (actionContext.ModelState.IsValid) return;

            var response = CreateBaseApiResponse(actionContext);
            actionContext.Result = new BadRequestObjectResult(response);
        }

        /// <summary>
        /// Создать ответ с ошибками модели
        /// </summary>
        private static BaseApiResponse CreateBaseApiResponse(ActionExecutingContext actionContext)
        {
            var response = new BaseApiResponse();
            response.AddModelStateError(actionContext.ModelState);
            return response;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError); //new BadRequestObjectResult(new BaseApiResponse().WithError(SystemErrorCodes.SystemError));
                return;
            }

            if (context.Result == null)
            {
                base.OnActionExecuted(context);
                return;
            }

            // смотрим что у нас в ответе
            //убрал as ObjectContent
            var responseObject = context.Result as ObjectResult;
            if (responseObject == null)
            {
                base.OnActionExecuted(context);
                return;
            }
            var contentObject = responseObject.Value;
            ProcessApiResponse(contentObject, context);

            base.OnActionExecuted(context);
        }

        private static void ProcessApiResponse(object? contentObject, ActionExecutedContext context)
        {
            if (contentObject is not BaseApiResponse)
            {
                return;
            }
            BaseApiResponse? response = contentObject as BaseApiResponse;
            if (response != null && response.HasError())
            {
                var httpStateCode = HttpStatusCode.UnprocessableEntity;
                if (response.HasError((int)SystemErrorCodes.SystemError))
                {
                    httpStateCode = HttpStatusCode.InternalServerError;
                }

                if (response.HasError((int)SystemErrorCodes.InvalidRequest))
                {
                    httpStateCode = HttpStatusCode.BadRequest;
                }

                context.HttpContext.Response.StatusCode = (int)httpStateCode;
            }
        }
    }
}
