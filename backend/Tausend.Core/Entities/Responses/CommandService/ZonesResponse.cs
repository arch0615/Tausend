using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Responses;

namespace Tausend.Core.Entities.Responses.CommandService
{
    public class ZonesResponse : BaseResponse
    {
        [DataMember]
        public string Open { get; set; }
        public string Exclusion { get; set; }
        public ZonesResponse() : base()
        {
        }
    }
}