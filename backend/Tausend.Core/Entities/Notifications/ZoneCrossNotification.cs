using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class ZoneCrossNotification : NotificationBase
    {
        public override string Name { get { return "Cruce de zonas"; } }

        public override NotificationTypes Type { get { return NotificationTypes.ZoneCross; } }
    }
}