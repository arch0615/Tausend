using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class MemoryDisarmedNotification : NotificationBase
    {
        public override string Name { get { return "Desarmado con memorias"; } }

        public override NotificationTypes Type { get { return NotificationTypes.MemoryDisarmed; } }
    }
}