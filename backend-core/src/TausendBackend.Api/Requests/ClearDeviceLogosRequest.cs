namespace TausendBackend.Api.Requests
{
    public class ClearDeviceLogosRequest
    {
        public string AccessToken { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string Pin { get; set; } = "";
    }
}
