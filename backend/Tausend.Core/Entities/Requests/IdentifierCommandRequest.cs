using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Models
{
    public class IdentifierCommandRequest
    {
        [DataMember]
        public string Identifier { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public string Command { get; set; }
        [DataMember]
        public string Pin { get; set; }
    }
}