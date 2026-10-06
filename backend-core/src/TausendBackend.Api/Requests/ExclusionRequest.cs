namespace TausendBackend.Api.Requests
{
    public class ExclusionRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public List<int> Zones { get; set; } = new();
    }
}
