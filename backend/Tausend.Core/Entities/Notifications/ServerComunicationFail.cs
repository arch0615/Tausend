using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class ServerComunicationFail : NotificationBase
    {
        public override string Name { get { return "Falla de conexión con el servidor"; } }
        public override NotificationTypes Type { get { return NotificationTypes.ServerComunicationFail; } }
        protected override bool IsFail { get { return true; } }
    }
}