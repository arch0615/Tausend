using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class StoleNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Stole; } }
        public override string Name { get { return "Robo"; } }
        public override bool UseZoneParam { get { return true; } }
    }
}