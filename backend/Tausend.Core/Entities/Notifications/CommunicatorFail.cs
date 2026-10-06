using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class CommunicatorFail : NotificationBase
    {
        public override string Name { get { return "Falla de comunicador"; } }

        public override NotificationTypes Type { get { return NotificationTypes.CommunicatorFail; } }
        protected override bool IsFail { get { return true; } }
    }
}