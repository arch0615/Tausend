using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Responses.DeviceService
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class DeletedDeviceResponse : BaseResponse
    {
        public DeletedDeviceResponse() : base()
        {
        }
    }
    public class DisassociateCentralResponse : BaseResponse
    {
        public DisassociateCentralResponse() : base()
        {
        }
    }

}