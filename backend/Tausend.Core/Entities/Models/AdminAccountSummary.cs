using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Models
{
    public class AdminAccountSummary
    {
        public long AccountId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public AccountRole Role { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
