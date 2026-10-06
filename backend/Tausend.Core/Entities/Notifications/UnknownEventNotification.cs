using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class UnknownEventNotification : NotificationBase
    {
        public override string Name { get { return "Evento desconocido"; } }

        public override NotificationTypes Type { get { return NotificationTypes.UnknownEvent; } }
    }
}