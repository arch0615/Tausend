using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class KeypadAssaultNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.KeypadAssault; } }
        public override string Name { get { return "Asalto por teclado"; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}