namespace TausendBackend.Api.Requests
{
    public class UnlinkAccountDeviceRequest
    {
        public string AccessToken { get; set; } = "";
        public long AccountId { get; set; }
        public long DeviceId { get; set; }
    }
}
