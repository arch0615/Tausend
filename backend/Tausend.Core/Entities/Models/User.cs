using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Entities.Models
{
    public class User
    {
        public long UserId { get; set; }
        public int UserNumber { get; set; }
        public string UserName { get; set; }
        public long DeviceId { get; set; }
    }
}