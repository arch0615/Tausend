using TausendBackend.Api.Models;

namespace TausendBackend.Api.Requests
{
    public class CreateZoneRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public List<Zone> Zones { get; set; } = new();
    }
}
