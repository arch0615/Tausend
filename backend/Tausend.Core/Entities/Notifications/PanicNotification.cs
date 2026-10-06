using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class PanicNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Panic;  } }
        public override string Name { get { return "Pánico"; } }
        public override bool UseDualParam { get { return true; } }
        public override bool UseZoneParam { get { return true; } }
        public override bool UseKeypadParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Se activó la alarma de pánico";
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}