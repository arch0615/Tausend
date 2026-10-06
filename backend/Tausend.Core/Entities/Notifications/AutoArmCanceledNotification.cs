using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class AutoArmCanceledNotification : NotificationBase
    {
        public override string Name { get { return ""; } }
        public override NotificationTypes Type { get { return NotificationTypes.AutoArmCanceled; } }
        public override bool UseUserParam { get { return true; } }
    }
}