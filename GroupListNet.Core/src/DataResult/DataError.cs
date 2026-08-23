using GroupListNet.Core.src.Enums.ErrorCodes;

namespace GroupListNet.Core.src.DataResult
{
    public class DataError : IDataError
    {

        public DataError(int code)
        {
            Code = code;
            Message = ErrorMessageManager.GetErrorMessage(code);
        }

        public DataError(int code, string message) : this(code)
        {
            Message = message;
        }

        public DataError(Enum @enum, params string[] replaces)
        {
            Code = Convert.ToInt32(@enum);
            Message = ErrorMessageManager.GetErrorMessage(Code);
            Replaces = replaces;
        }

        public int Code { get; private set; }

        public string Message { get; private set; }

        public string[] Replaces { get; private set; } = [];
    }
}