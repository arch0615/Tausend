using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class BatteryFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de batería"; } }

        public override NotificationTypes Type { get { return NotificationTypes.BatteryFail; } }
        protected override bool IsFail { get { return true; } }
    }
}