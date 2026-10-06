namespace TausendBackend.Api.Requests
{
    public class CreateDeviceRequest
    {
        public string AccessToken { get; set; } = "";
        public string Description { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string Pin { get; set; } = "";
        public string? Email { get; set; }
    }
}
