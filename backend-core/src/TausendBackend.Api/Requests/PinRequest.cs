namespace TausendBackend.Api.Requests
{
    public class PinRequest
    {
        public string Identifier { get; set; } = "";
        public string Action { get; set; } = "";
        public string AccessToken { get; set; } = "";
    }
}
