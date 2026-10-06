namespace TausendBackend.Api.Models
{
    public class User
    {
        public long UserId { get; set; }
        public int UserNumber { get; set; }
        public string? UserName { get; set; }
        public long DeviceId { get; set; }
    }
}
