using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class SabotageNotification : NotificationBase
    {
        public override string Name { get { return "Sabotaje"; } }
        public override NotificationTypes Type { get { return NotificationTypes.Sabotage; } }
        public override bool UseZoneParam { get { return true; } }
    }
}