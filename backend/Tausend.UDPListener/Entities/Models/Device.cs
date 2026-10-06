using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Tausend.Backend.Models
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class Device
    {
        [IgnoreDataMember]
        public long DeviceId { get; set; } 
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string IP { get; set; }
        [DataMember]
        public string Port { get; set; }
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public bool IsOnline { get; set; }
        [DataMember]
        public DateTime LastConnection { get; set; }
        [DataMember]
        public ushort PublicKey { get; set; }
    }
}
