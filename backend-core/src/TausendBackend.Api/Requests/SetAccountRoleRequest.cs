using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Requests
{
    public class SetAccountRoleRequest
    {
        public string AccessToken { get; set; } = "";
        public long AccountId { get; set; }
        public AccountRole Role { get; set; }
    }
}
