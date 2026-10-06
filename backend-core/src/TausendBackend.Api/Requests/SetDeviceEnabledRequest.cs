namespace TausendBackend.Api.Requests
{
    public class SetDeviceEnabledRequest
    {
        public string AccessToken { get; set; } = "";
        public long DeviceId { get; set; }
        public bool Enabled { get; set; }
    }
}
