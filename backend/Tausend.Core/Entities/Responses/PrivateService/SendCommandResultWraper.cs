using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Responses.PrivateService
{
    public class SendCommandResultWraper
    {
        [DataMember]
        public SendCommandResult SendCommandResult { get; set; }
    }
}