namespace GroupListNet.Core.src.DataResult
{
    public class DataResult : IDataResult
    {
        public DataResult()
        {
            Errors = new List<IDataError>();
        }

        public IList<IDataError> Errors { get; private set; }

        public bool Success
        {
            get { return !Errors.Any(); }
        }
    }
}