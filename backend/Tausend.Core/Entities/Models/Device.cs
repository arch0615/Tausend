using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Tausend.Backend.Models
{
    public class Device
    {
        public long DeviceId { get; set; }
        public string Description { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public string Identifier { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastConnection { get; set; }
        public ushort PublicKey { get; set; }
        public string AccessToken { get; set; }
    }
    public class DeviceDisassociate
    {
        public string Identifier { get; set; }
        public string AccessToken { get; set; }
    }
}
