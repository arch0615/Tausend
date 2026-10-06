namespace TausendBackend.Api.Requests
{
    public class UpdateDeviceRequest
    {
        public long DeviceId { get; set; }
        public string Description { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string Pin { get; set; } = "";
        public string AccessToken { get; set; } = "";
    }
}
