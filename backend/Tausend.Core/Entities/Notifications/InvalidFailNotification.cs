using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class InvalidFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla inválida"; } }

        public override NotificationTypes Type { get { return NotificationTypes.InvalidFail; } }
        protected override bool IsFail { get { return true; } }
    }
}