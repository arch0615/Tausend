namespace TausendBackend.Api.Models
{
    public class AccountDevice
    {
        public long DeviceId { get; set; }
        public string? Description { get; set; }
        public string? Mac { get; set; }
        public bool IsOnline { get; set; }
        public string? Pin { get; set; }
        public string? Logo1Base64 { get; set; }
        public string? Logo1MimeType { get; set; }
        public string? Logo2Base64 { get; set; }
        public string? Logo2MimeType { get; set; }
    }
}
