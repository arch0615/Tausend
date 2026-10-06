using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class BusFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de bus"; } }

        public override NotificationTypes Type { get { return NotificationTypes.BusFail; } }
        protected override bool IsFail { get { return true; } }
    }
}