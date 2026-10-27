namespace TausendBackend.Api.Requests
{
    public class SetDeviceLogosRequest
    {
        public string AccessToken { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string Pin { get; set; } = "";
        public string Logo1Base64 { get; set; } = "";
        public string Logo2Base64 { get; set; } = "";
    }
}
