using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class SilentPanicNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.SilentPanic; } }
        public override string Name { get { return "Asalto Silencioso"; } }
        public override bool UseKeypadParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Se activo \"Asalto Silencioso\"";
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}