using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Tausend.Core.Entities.Requests
{
    public class PinRequest
    {
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public string Action { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
    }
}