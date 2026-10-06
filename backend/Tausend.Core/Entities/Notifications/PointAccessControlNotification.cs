using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class PointAccessControlNotification : NotificationBase
    {
        public override string Name { get { return "Control de acceso punto"; } }
        public override NotificationTypes Type { get { return NotificationTypes.PointAccessControl; } }
        public override bool UsePointParam { get { return true; } }
    }
}