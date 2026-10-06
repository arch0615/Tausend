using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class CreatedDeviceResponse : BaseResponse
    {
    }

    public class NewCreatedDeviceResponse : BaseResponse
    {
        public AccountDevice? Device { get; set; }
    }
}
