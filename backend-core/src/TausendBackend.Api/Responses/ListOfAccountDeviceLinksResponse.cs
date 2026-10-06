using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfAccountDeviceLinksResponse : BaseResponse
    {
        public List<AccountDeviceLink>? Links { get; set; }

        public ListOfAccountDeviceLinksResponse(List<AccountDeviceLink>? links)
        {
            Links = links;
        }
    }
}
