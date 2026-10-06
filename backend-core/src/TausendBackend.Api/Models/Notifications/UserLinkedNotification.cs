using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class UserLinkedNotification : NotificationBase
    {
        public override string Name { get { return "Usuario vinculado"; } }
        public override NotificationTypes Type { get { return NotificationTypes.UserLinked; } }
        protected override string GetBody()
        {
            string result = string.Format("Usuario {0} ha vinculado su dispositivo a la Central.", Email);
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
