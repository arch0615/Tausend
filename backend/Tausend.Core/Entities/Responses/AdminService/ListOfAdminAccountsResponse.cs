using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Entities.Responses.AdminService
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class ListOfAdminAccountsResponse : BaseResponse
    {
        [DataMember]
        public List<AdminAccountSummary> Accounts { get; set; }

        public ListOfAdminAccountsResponse(List<AdminAccountSummary> accounts)
        {
            Accounts = accounts;
        }
    }
}
