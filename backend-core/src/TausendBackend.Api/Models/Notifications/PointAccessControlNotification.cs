using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class PointAccessControlNotification : NotificationBase
    {
        public override string Name { get { return "Control de acceso punto"; } }
        public override NotificationTypes Type { get { return NotificationTypes.PointAccessControl; } }
        public override bool UsePointParam { get { return true; } }
    }
}
