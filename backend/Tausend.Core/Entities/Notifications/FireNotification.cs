using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class FireNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Fire; } }
        public override string Name { get { return "Fuego"; } }
        public override bool UseDualParam { get { return true; } }
        public override bool UseZoneParam { get { return true; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}