using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class UserAccessControlNotification : NotificationBase
    {
        public override string Name { get { return "Control de acceso usuario"; } }
        public override NotificationTypes Type { get { return NotificationTypes.UserAccessControl; } }
        public override bool UseUserParam { get { return true; } }
    }
}
