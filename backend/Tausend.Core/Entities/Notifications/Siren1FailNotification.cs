using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class Siren1FailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de sirena 1"; } }

        public override NotificationTypes Type { get { return NotificationTypes.Siren1Fail; } }
        protected override bool IsFail { get { return true; } }
    }
}