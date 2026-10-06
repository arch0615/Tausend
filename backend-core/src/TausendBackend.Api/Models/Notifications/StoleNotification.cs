using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class StoleNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Stole; } }
        public override string Name { get { return "Robo"; } }
        public override bool UseZoneParam { get { return true; } }
    }
}
