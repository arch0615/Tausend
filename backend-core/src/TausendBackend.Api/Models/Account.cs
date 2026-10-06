using System.Text.Json.Serialization;
using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models
{
    public class Account
    {
        public Account()
        {
            Devices = new List<AccountDevice>();
            RemovedDevices = new List<AccountDevice>();
            OfflineDevices = new List<AccountDevice>();
            SmsDevices = new List<AccountSmsDevice>();
        }

        [JsonIgnore]
        public long AccountId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public AccountRole Role { get; set; }
        public List<AccountDevice> Devices { get; set; }
        public List<AccountSmsDevice> SmsDevices { get; set; }
        public List<AccountDevice> RemovedDevices { get; set; }
        public List<AccountDevice> OfflineDevices { get; set; }
    }
}
