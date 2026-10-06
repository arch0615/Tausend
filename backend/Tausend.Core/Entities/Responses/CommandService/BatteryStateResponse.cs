using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Responses;

namespace Tausend.Core.Entities.Responses.CommandService
{
    public class BatteryStateResponse : BaseResponse
    {
        public float InTension { get; set; }
        public float ChargeTension { get; set; }
        public float Current { get; set; }
        public float TestTension { get; set; }
    }
}