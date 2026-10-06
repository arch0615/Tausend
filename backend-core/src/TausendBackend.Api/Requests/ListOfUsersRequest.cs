using TausendBackend.Api.Models;

namespace TausendBackend.Api.Requests
{
    public class ListOfUsersRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public List<User> Users { get; set; } = new();
    }
}
