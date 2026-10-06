using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class PhoneLineFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de línea telefónica"; } }

        public override NotificationTypes Type { get { return NotificationTypes.PhoneLineFail; } }
        protected override bool IsFail { get { return true; } }
    }
}