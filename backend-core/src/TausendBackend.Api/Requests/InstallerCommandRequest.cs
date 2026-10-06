namespace TausendBackend.Api.Requests
{
    public class InstallerCommandRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public string Command { get; set; } = "";
    }
}
