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
    public class ListOfAdminDevicesResponse : BaseResponse
    {
        [DataMember]
        public List<AdminDeviceSummary> Devices { get; set; }

        public ListOfAdminDevicesResponse(List<AdminDeviceSummary> devices)
        {
            Devices = devices;
        }
    }
}
