using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfScheduledPgmActionsResponse : BaseResponse
    {
        public List<ScheduledPgmAction>? Schedules { get; private set; }

        public ListOfScheduledPgmActionsResponse(List<ScheduledPgmAction>? schedules)
        {
            Schedules = schedules;
        }
    }
}
