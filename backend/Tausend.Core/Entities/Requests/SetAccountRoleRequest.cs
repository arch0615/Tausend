using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Requests
{
    public class SetAccountRoleRequest
    {
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public long AccountId { get; set; }
        [DataMember]
        public AccountRole Role { get; set; }
    }
}
