namespace TausendBackend.Api.Requests
{
    public class CommandRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
    }
}
