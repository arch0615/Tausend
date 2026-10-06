using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfEventsResponse : BaseResponse
    {
        public List<Event>? Events { get; private set; }

        public ListOfEventsResponse(List<Event>? events)
        {
            Events = events;
        }
    }
}
