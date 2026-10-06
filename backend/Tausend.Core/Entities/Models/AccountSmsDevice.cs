using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Tausend.Backend.Models
{
    public class AccountSmsDevice
    {
        [DataMember]
        public long DeviceId { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public string DevicePin { get; set; }
        [DataMember]
        public string SimPin { get; set; }
        [DataMember]
        public string PhoneNumber { get; set; }
        [DataMember]
        public string DeviceType { get; set; }
    }
}
