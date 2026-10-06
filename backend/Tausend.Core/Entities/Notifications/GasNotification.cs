using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class GasNotification : NotificationBase
    {
        public override string Name { get { return "Gas"; } }
        public override NotificationTypes Type { get { return NotificationTypes.Gas; } }
        public override bool UseZoneParam { get { return true; } }
    }
}