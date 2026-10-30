namespace TausendBackend.Api.Requests
{
    public class SetNotificationMuteRequest
    {
        public string AccessToken { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string DeviceToken { get; set; } = "";
        public bool Muted { get; set; }
    }
}
