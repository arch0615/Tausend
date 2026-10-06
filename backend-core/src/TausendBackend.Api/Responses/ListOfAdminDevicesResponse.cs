using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfAdminDevicesResponse : BaseResponse
    {
        public List<AdminDeviceSummary>? Devices { get; set; }

        public ListOfAdminDevicesResponse(List<AdminDeviceSummary>? devices)
        {
            Devices = devices;
        }
    }
}
