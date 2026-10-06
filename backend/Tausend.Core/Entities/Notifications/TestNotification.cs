using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class TestNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Test; } }
        public override string Title { get { return "Testing"; } }
        public override string Name { get { return "Testing"; } }
        protected override string GetBody() 
        { 
            return "Notificacion de prueba de Tausend"; 
        }
    }
}