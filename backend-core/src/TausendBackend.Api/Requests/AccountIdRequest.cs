namespace TausendBackend.Api.Requests
{
    public class AccountIdRequest
    {
        public string AccessToken { get; set; } = "";
        public long AccountId { get; set; }
    }
}
