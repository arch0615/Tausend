using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class GasNotification : NotificationBase
    {
        public override string Name { get { return "Gas"; } }
        public override NotificationTypes Type { get { return NotificationTypes.Gas; } }
        public override bool UseZoneParam { get { return true; } }
    }
}
