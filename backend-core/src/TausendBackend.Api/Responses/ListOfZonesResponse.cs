using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfZonesResponse : BaseResponse
    {
        public List<Zone>? Zones { get; private set; }

        public ListOfZonesResponse(List<Zone>? zones)
        {
            Zones = zones;
        }
    }
}
