using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class ProgramNotification : NotificationBase
    {
        public override string Name { get { return "Programación"; } }
        public override NotificationTypes Type { get { return NotificationTypes.Program; } }
        public override bool UseUserParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Programación: " + User.ToString();
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}