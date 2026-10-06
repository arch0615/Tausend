using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class KeyArmDisarmNotification : NotificationBase
    {
        public override string Name { get { return GetName(); } }
        public override NotificationTypes Type { get { return NotificationTypes.KeyArmDisarm; } }
        public override bool UseZoneParam { get { return true; } }

        private string GetName()
        {
            if (IsReplacement())
            {
                return "Armado por llave";
            }
            else if (IsEvent())
            {
                return "Desarmado por llave";
            }
            else
            {
                return "Armado/Desarmado por llave";
            }
        }

        protected override string GetBody()
        {
            string result;
            if (IsReplacement())
            {
                result = String.Format("La alarma se armó por llave en la zona {0}", Zone);
                result = String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
            }
            else if (IsEvent())
            {
                result = String.Format("La alarma se desarmó por llave en la zona {0}", Zone);
                result = String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
            }
            else
            {
                result = base.GetBody();
            }
            return result;
        }
    }
}