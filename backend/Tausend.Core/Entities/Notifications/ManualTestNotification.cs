using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class ManualTestNotification : NotificationBase
    {
        public override string Name { get { return "Test manual"; } }
        public override NotificationTypes Type { get { return NotificationTypes.ManualTest; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}