using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class ZoneBypassNotification : NotificationBase
    {
        public override string Name { get { return "Bypass de zonas"; } }
        public override NotificationTypes Type { get { return NotificationTypes.ZoneBypass; } }
        public override bool UseZoneParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Exclusión de zona " + Zone;
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}