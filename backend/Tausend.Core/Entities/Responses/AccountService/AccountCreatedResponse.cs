using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class AccountCreatedResponse : BaseResponse
    {
        public AccountCreatedResponse() : base()
        {
        }
    }
}