namespace TausendBackend.Api.Requests
{
    public class SetAccountEnabledRequest
    {
        public string AccessToken { get; set; } = "";
        public long AccountId { get; set; }
        public bool Enabled { get; set; }
    }
}
