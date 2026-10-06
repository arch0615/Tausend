using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Backend.Models;

namespace Tausend.Core.Responses.DeviceService
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class CreatedDeviceResponse : BaseResponse
    {
        public CreatedDeviceResponse() : base() { }
    }

    public class NewCreatedDeviceResponse : BaseResponse
    {
        public AccountDevice Device { get; set; }
        public NewCreatedDeviceResponse() : base() { }
    }
}