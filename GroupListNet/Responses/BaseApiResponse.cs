using GroupListNet.Core.src.DataResult;

namespace GroupListNet.Web.Api.Responses
{
    public class BaseApiResponse
    {
        public BaseApiResponse()
        {
            Errors = new List<IDataError>();
        }

        public IList<IDataError> Errors { get; private set; }
    }
}