using Microsoft.AspNetCore.Mvc.ModelBinding;
using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Enums.ErrorCodes;
using GroupListNet.Utils.src.Extensions;
using GroupListNet.Web.Api.Responses;

namespace GroupListNet.Web.Api.Extensions
{
    public static class BaseApiResponseExtensions
    {
        public static T WithErrors<T>(this T response, IEnumerable<IDataError> errors) where T : BaseApiResponse
        {
            ArgumentChecker.NotNull(response, nameof(response));
            ArgumentChecker.NotNull(errors, nameof(errors));

            errors.Foreach(x => response.Errors.Add(x));

            return response;
        }

        public static T WithError<T>(this T response, IDataError error) where T : BaseApiResponse
        {
            ArgumentChecker.NotNull(response, nameof(response));
            ArgumentChecker.NotNull(error, nameof(error));

            response.Errors.Add(error);

            return response;
        }

        public static T WithError<T>(this T response, string msg) where T : BaseApiResponse
        {
            ArgumentChecker.NotNull(response, nameof(response));
            ArgumentChecker.NotNull(msg, nameof(msg));
            var error = new DataError(100, msg);
            response.Errors.Add(error);

            return response;
        }


        public static T WithError<T>(this T response, int errorCode, string msg = "") where T : BaseApiResponse
        {
            ArgumentChecker.NotNull(response, nameof(response));
            if (string.IsNullOrEmpty(msg))
                response.Errors.Add(new DataError(errorCode));
            else
                response.Errors.Add(new DataError(errorCode, msg));

            return response;
        }

        public static T WithError<T>(this T response, Enum errorCode) where T : BaseApiResponse
        {
            ArgumentChecker.NotNull(response, nameof(response));

            response.Errors.Add(new DataError(errorCode));

            return response;
        }

        public static bool HasError(this BaseApiResponse response, int errorCode)
        {
            return response.Errors.Any(x => x.Code == errorCode);
        }

        public static bool HasError(this BaseApiResponse response)
        {
            return response.Errors.Any();
        }

        public static T AddModelStateError<T>(this T response, ModelStateDictionary modelState) where T : BaseApiResponse
        {
            foreach (var error in modelState.Values.SelectMany(e => e.Errors))
            {
                response.WithError(new DataError(SystemErrorCodes.InvalidRequest, error.ErrorMessage));
            }

            return response;
        }

        public static DataResponse<T> WithData<T>(this DataResponse<T> response, T data)
        {
            response.Data = data;

            return response;
        }
    }
}