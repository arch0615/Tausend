using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Entities.Requests
{
    public class DeviceTokenRequest
    {
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public string DeviceToken { get; set; }
    }
}