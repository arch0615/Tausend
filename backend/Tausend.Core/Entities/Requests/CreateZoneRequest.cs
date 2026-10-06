using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Entities.Requests
{
    public class CreateZoneRequest
    {
        [DataMember]
        public long DeviceId { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public List<Zone> Zones { get; set; }
    }
}