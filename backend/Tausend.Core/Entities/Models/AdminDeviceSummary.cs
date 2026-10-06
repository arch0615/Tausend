using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Entities.Models
{
    public class AdminDeviceSummary
    {
        public long DeviceId { get; set; }
        public string Description { get; set; }
        public string Identifier { get; set; }
        public bool Enabled { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastConnection { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
