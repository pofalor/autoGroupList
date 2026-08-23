namespace GroupListNet.Web.Api.Responses
{
    public class DataResponse<T> : BaseApiResponse
    {
        public DataResponse()
        {
        }

        public DataResponse(T data)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}