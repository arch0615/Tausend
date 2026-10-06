using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Models
{
    public class UpdateDeviceRequest
    {
        public long DeviceId { get; set; }
        public string Description { get; set; }
        public string Identifier { get; set; }
        public string Pin { get; set; }
        public string AccessToken { get; set; }
    }
}