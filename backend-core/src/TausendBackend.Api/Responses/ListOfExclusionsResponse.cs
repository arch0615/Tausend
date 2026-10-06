using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfExclusionsResponse : BaseResponse
    {
        public List<Exclusion>? Exclusions { get; private set; }

        public ListOfExclusionsResponse(List<Exclusion>? exclusions)
        {
            Exclusions = exclusions;
        }
    }
}
