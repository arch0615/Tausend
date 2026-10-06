using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class AutoArm : NotificationBase
    {
        public override string Name { get { return "Armado automático"; } }

        public override NotificationTypes Type { get { return NotificationTypes.AutoArm; } }
    }
}
