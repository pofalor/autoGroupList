using GroupListNet.Utils.src.Extensions;

namespace GroupListNet.Core.src.DataResult
{
    public static class DataResultExtensions
    {
        public static IDataResult<T> WithData<T>(this IDataResult<T> dataResult, T data)
        {
            dataResult.Data = data;
            return dataResult;
        }

        public static IDataResult<T> WithError<T>(this IDataResult<T> dataResult, string message = "")
        {
            dataResult.Errors.Add(new DataError(500, message));
            return dataResult;
        }

        public static IDataResult<T> WithError<T>(this IDataResult<T> dataResult, int errorCode, string message = "")
        {
            dataResult.Errors.Add(new DataError(errorCode, message));

            return dataResult;
        }

        public static IDataResult<T> WithError<T>(this IDataResult<T> dataResult, Enum errorCode, params string[] replaces)
        {
            dataResult.Errors.Add(new DataError(errorCode, replaces));

            return dataResult;
        }

        public static IDataResult WithError(this IDataResult dataResult, Enum errorCode)
        {
            dataResult.Errors.Add(new DataError(errorCode));

            return dataResult;
        }

        public static IDataResult WithError(this IDataResult dataResult, string message = "")
        {
            dataResult.Errors.Add(new DataError(500, message));
            return dataResult;
        }

        public static string GetReplacedError(this IDataError dataError)
        {
            return string.Format(dataError.Message, dataError.Replaces);
        }
    }
}