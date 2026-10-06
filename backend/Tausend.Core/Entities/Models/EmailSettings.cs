using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Models
{
    public class EmailSettings
    {
        public string SMTPHostName { get; set; }
        public int SMTPPort { get; set; }
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }
        public string SenderName { get; set; }
        public bool UseSSL { get { return !String.IsNullOrEmpty(SenderPassword); } }
    }
}