using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class ManualTestNotification : NotificationBase
    {
        public override string Name { get { return "Test manual"; } }
        public override NotificationTypes Type { get { return NotificationTypes.ManualTest; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}
