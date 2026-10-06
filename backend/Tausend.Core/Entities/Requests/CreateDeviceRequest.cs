using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Models
{
    public class CreateDeviceRequest
    {
        public string AccessToken { get; set; }
        public string Description { get; set; }
        public string Identifier { get; set; }
        public string Pin { get; set; }
        public string Email { get; set; }
    }
}