using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Entities.Models
{
    public class Exclusion
    {
        public long ExclusionId { get; set; }
        public long DeviceId { get; set; }
        public int ExclusionNumber { get; set; }
        public string Name { get; set; }
        public bool Excluded { get; set; }
        public bool Open { get; set; }
    }
}