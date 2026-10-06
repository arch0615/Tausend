using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Entities.Models
{
    public class ProgramControl
    {
        public long ProgramControlId { get; set; }
        public long DeviceId { get; set; }
        public int ProgramControlNumber { get; set; }
        public string Name { get; set; }
        public bool Activated { get; set; }
    }
}