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
    public class ListOfAuditLogResponse : BaseResponse
    {
        [DataMember]
        public List<AuditLogEntry> Entries { get; set; }

        public ListOfAuditLogResponse(List<AuditLogEntry> entries)
        {
            Entries = entries;
        }
    }
}
