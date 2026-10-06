using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Models
{
    public class Email
    {
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string ToEmail { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}