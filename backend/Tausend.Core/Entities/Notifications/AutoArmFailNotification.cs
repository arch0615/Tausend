using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class AutoArmFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de armado automático"; } }
        public override NotificationTypes Type { get { return NotificationTypes.AutoArmFail; } }
        protected override bool IsFail { get { return true; } }
    }
}