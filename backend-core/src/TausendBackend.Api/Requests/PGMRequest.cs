namespace TausendBackend.Api.Requests
{
    public class PGMRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public int Zone { get; set; }
        public bool State { get; set; }
    }
}
