namespace TausendBackend.Api.Models
{
    public class AccountDevice
    {
        public long DeviceId { get; set; }
        public string? Description { get; set; }
        public string? Mac { get; set; }
        public bool IsOnline { get; set; }
        public string? Pin { get; set; }
    }
}
