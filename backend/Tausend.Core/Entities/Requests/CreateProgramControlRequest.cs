using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Entities.Requests
{
    public class PostProgramControlRequest
    {
        [DataMember]
        public long DeviceId { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public List<ProgramControl> ProgramControls { get; set; }
    }
}