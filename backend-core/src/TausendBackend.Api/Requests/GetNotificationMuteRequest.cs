namespace TausendBackend.Api.Requests
{
    public class GetNotificationMuteRequest
    {
        public string AccessToken { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string DeviceToken { get; set; } = "";
    }
}
