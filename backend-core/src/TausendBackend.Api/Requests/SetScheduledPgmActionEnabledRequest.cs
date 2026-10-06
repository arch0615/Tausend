namespace TausendBackend.Api.Requests
{
    public class SetScheduledPgmActionEnabledRequest
    {
        public string AccessToken { get; set; } = "";
        public long DeviceId { get; set; }
        public long ScheduledPgmActionId { get; set; }
        public bool Enabled { get; set; }
    }
}
