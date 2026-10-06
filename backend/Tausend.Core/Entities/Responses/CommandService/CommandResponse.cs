using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class CommandResponse : BaseResponse
    {
        [DataMember]
        public string Text { get; set; }
        public CommandResponse() : base()
        {
        }
    }
}