using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Entities.Requests
{
    public class ResetPasswordRequest
    {
        [DataMember]
        public string ResetToken { get; set; }
        [DataMember]
        public string NewPassword { get; set; }
    }
}
