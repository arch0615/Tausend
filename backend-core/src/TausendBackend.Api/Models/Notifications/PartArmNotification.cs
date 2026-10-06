using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class PartArmNotification : NotificationBase
    {
        public override string Name { get { return "Armado parcial"; } }

        public override NotificationTypes Type { get { return NotificationTypes.PartArm; } }
    }
}
