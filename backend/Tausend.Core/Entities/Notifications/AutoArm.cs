using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class AutoArm : NotificationBase
    {
        public override string Name { get { return "Armado automático"; } }

        public override NotificationTypes Type { get { return NotificationTypes.AutoArm; } }
    }
}