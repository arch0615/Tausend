using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class AssaultNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Assault; } }
        public override string Name { get { return "Asalto"; } }
        public override bool UseDualParam { get { return true; } }
        public override bool UseUserParam { get { return true; } }
        public override bool UseZoneParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Asalto silencioso";
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}