using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class VACLineFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de línea 220VAC"; } }

        public override NotificationTypes Type { get { return NotificationTypes.VACLineFail; } }
        protected override bool IsFail { get { return true; } }
    }
}