namespace TausendBackend.Api.Models
{
    public class Zone
    {
        public long ZoneId { get; set; }
        public long DeviceId { get; set; }
        public int ZoneNumber { get; set; }
        public string? Name { get; set; }
        public bool Excluded { get; set; }
        public bool Open { get; set; }
    }
}
