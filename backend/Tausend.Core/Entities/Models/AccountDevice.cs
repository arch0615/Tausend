using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Tausend.Backend.Models
{
    public class AccountDevice
    {
        [DataMember]
        public long DeviceId { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Mac { get; set; }
        [DataMember]
        public bool IsOnline { get; set; }
        [DataMember]
        public string Pin { get; set; }
    }
}
