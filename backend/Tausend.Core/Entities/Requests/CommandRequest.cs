using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Models
{
    public class CommandRequest
    {
        [DataMember]
        public long DeviceId { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
    }
}