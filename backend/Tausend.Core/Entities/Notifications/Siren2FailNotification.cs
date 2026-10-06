using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class Siren2FailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de sirena 2"; } }

        public override NotificationTypes Type { get { return NotificationTypes.Siren2Fail; } }
        protected override bool IsFail { get { return true; } }
    }
}