using GroupListNet.Utils.src.Extensions;

namespace GroupListNet.Core.src.DataResult
{
    public class DataResult<T> : IDataResult<T>
    {
        public DataResult()
        {
            Errors = [];
        }

        public DataResult(T data) : this()
        {
            Data = data;
        }

        public T Data { get; set; } = default!;

        public IList<IDataError> Errors { get; private set; }

        public bool Success
        {
            get { return !Errors.Any(); }
        }
    }
}