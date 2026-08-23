using System.Web;

namespace GroupListNet.Core.src.Models.PostRequests
{
    public class BasePostRequest
    {
        public string IP { get; set; } = string.Empty;

        public string Localization { get; set; } = string.Empty;
    }
}
