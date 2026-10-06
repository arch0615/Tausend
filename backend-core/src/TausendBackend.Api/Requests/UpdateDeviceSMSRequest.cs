namespace TausendBackend.Api.Requests
{
    public class UpdateDeviceSMSRequest
    {
        public long DeviceId { get; set; }
        public string Description { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string DevicePin { get; set; } = "";
        public string SimPin { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string AccessToken { get; set; } = "";
    }
}
