using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class RecentArmedAlarmNotification : NotificationBase
    {
        public override string Name { get { return "Alarma con armado reciente"; } }

        public override NotificationTypes Type { get { return NotificationTypes.RecentArmedAlarm; } }

        protected override string GetBody()
        {
            var result = "Se activo \"Alarma con armado reciente\"";
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}