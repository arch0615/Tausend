using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class PeriodicTestNotification : NotificationBase
    {
        public override string Name { get { return "Test periódico"; } }

        public override NotificationTypes Type { get { return NotificationTypes.PeriodicTest; } }
    }
}