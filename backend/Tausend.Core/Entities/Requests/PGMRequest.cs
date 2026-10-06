using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Models
{
    public class PGMRequest
    {
        [DataMember]
        public long DeviceId { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public int Zone { get; set; }
        [DataMember]
        public bool State { get; set; }
    }
}