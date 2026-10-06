using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Tausend.Core.Enums;

namespace Tausend.Backend.Models
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
        [IgnoreDataMember]
        public long AccountId { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public string RefreshToken { get; set; }
        [DataMember]
        public AccountRole Role { get; set; }
        [DataMember]
        public List<AccountDevice> Devices { get; set; }
        [DataMember]
        public List<AccountSmsDevice> SmsDevices { get; set; }
        [DataMember]
        public List<AccountDevice> RemovedDevices { get; set; }
        [DataMember]
        public List<AccountDevice> OfflineDevices { get; set; }

    }
}
