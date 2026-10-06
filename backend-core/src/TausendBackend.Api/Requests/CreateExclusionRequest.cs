using TausendBackend.Api.Models;

namespace TausendBackend.Api.Requests
{
    public class CreateExclusionRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public List<Exclusion> Exclusions { get; set; } = new();
    }
}
