using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class SabotageNotification : NotificationBase
    {
        public override string Name { get { return "Sabotaje"; } }
        public override NotificationTypes Type { get { return NotificationTypes.Sabotage; } }
        public override bool UseZoneParam { get { return true; } }
    }
}
