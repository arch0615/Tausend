using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class ClockFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de reloj"; } }

        public override NotificationTypes Type { get { return NotificationTypes.ClockFail; } }
        protected override bool IsFail { get { return true; } }
    }
}