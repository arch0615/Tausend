using System;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class UserLinkedNotification : NotificationBase
    {
        public override string Name { get { return "Usuario vinculado"; } }
        public override NotificationTypes Type { get { return NotificationTypes.UserLinked; } }
        protected override string GetBody()
        {
            string result = String.Format("Usuario {0} ha vinculado su dispositivo a la Central.", Email);
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}