using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfUserResponse : BaseResponse
    {
        public List<User>? Users { get; set; }

        public ListOfUserResponse(List<User>? users)
        {
            Users = users;
        }
    }
}
