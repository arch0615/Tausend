namespace TausendBackend.Api.Requests
{
    public class UpdateAccountRequest
    {
        public string AccessToken { get; set; } = "";
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
