using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models
{
    public class DeviceToken
    {
        public long DeviceTokenId { get; set; }
        public long AccountId { get; set; }
        public string? Token { get; set; }
        public string OSDescription { get; set; } = "";
        public PhoneOS OS => OSDescription.ToPhoneType();
    }
}
