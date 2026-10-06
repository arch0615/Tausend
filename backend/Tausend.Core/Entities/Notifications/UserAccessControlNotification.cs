using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class UserAccessControlNotification : NotificationBase
    {
        public override string Name { get { return "Control de acceso usuario"; } }
        public override NotificationTypes Type { get { return NotificationTypes.UserAccessControl; } }
        public override bool UseUserParam { get { return true; } }
    }
}