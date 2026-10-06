using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class AccountDeletedResponse : BaseResponse
    {
        public AccountDeletedResponse() : base()
        {
        }
    }
}