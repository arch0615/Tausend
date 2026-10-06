using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfProgramControlResponse : BaseResponse
    {
        public List<ProgramControl>? ProgramControls { get; private set; }

        public ListOfProgramControlResponse(List<ProgramControl>? programControls)
        {
            ProgramControls = programControls;
        }
    }
}
