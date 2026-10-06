using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class SilentAlarm : NotificationBase
    {
        public override string Name { get { return "Alarma Silenciosa"; } }
        public override NotificationTypes Type { get { return NotificationTypes.SilentAlarm; } }
        public override bool UseZoneParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Se activo \"Asalto Silencioso\"";
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}