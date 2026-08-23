namespace GroupListNet.Core.src.DataResult
{
    public interface IDataError
    {
        int Code { get; }

        string Message { get; }

        string[] Replaces { get; }
    }
}