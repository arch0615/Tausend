namespace TausendBackend.Api.Requests
{
    public class ScheduledPgmActionIdRequest
    {
        public string AccessToken { get; set; } = "";
        public long DeviceId { get; set; }
        public long ScheduledPgmActionId { get; set; }
    }
}
