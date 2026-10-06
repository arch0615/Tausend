using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class FastArmNotification : NotificationBase
    {
        public override string Name { get { return "Armado rápido"; } }
        public override NotificationTypes Type { get { return NotificationTypes.FastArm; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}
