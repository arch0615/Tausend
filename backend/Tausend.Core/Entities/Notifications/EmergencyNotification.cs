using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;
using Tausend.Core.Models;

namespace Tausend.Core.Entities.Notifications
{
    public class EmergencyNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Medical; } }
        public override string Name { get { return "Emergencia"; } }
        protected override string GetBody()
        {
            var result = "Se activó \"Emergencia\"";
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}