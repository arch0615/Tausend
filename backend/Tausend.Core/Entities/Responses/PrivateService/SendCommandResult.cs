using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Responses.PrivateService
{
    public class SendCommandResult
    {
        public ResponseStates State { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
        public string Text { get; set; }
        public SendCommandResult()
        {
        }
    }
}