using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Models
{
    public class DeviceToken
    {
        public long DeviceTokenId { get; set; }
        public long AccountId { get; set; }
        public string Token { get; set; }
        public string OSDescription { get; set; }
        public PhoneOS OS { get { return OSDescription.ToPhoneType(); } }
    }
}