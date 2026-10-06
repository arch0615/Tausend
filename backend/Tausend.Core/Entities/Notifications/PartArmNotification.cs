using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class PartArmNotification : NotificationBase
    {
        public override string Name { get { return "Armado parcial"; } }

        public override NotificationTypes Type { get { return NotificationTypes.PartArm; } }
    }
}