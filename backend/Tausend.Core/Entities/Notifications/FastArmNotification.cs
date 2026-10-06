using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class FastArmNotification : NotificationBase
    {
        public override string Name { get { return "Armado rápido"; } }
        public override NotificationTypes Type { get { return NotificationTypes.FastArm; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}