using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Models
{
    public class UpdateDeviceSMSRequest
    {
        public long DeviceId { get; set; }
        public string Description { get; set; }
        public string Identifier { get; set; }
        public string DevicePin { get; set; }
        public string SimPin { get; set; }
        public string PhoneNumber { get; set; }
        public string DeviceType { get; set; }
        public string AccessToken { get; set; }
    }
}