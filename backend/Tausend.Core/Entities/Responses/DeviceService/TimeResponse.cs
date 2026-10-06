using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Responses;

namespace Tausend.Core.Entities.Responses.DeviceService
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class TimeResponse : BaseResponse
    {
        [DataMember]
        public DateTime DeviceTime { get; set; }
        [DataMember]
        public DateTime ServerTime { get; set; }
    }
}