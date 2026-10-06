using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Entities.Requests
{
    public class AccessTokenRequest
    {
        [DataMember]
        public string AccessToken { get; set; }
    }
}