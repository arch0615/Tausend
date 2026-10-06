using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Backend.Models;
using Tausend.Core.Enums;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class AccountLoginResponse : BaseResponse
    {
        [DataMember]
        public Account Account { get; set; }
        [DataMember]
        public bool PinChanged { get; set; }

        public AccountLoginResponse() : base()
        {
            Account = null;
            PinChanged = false;
            InformServerError("No account selected for return");
        }

        public void SetAccount(Account account)
        {
            InformOk();
            Account = account;
        }

    }
}