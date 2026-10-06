using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class PeriodicTestNotification : NotificationBase
    {
        public override string Name { get { return "Test periódico"; } }

        public override NotificationTypes Type { get { return NotificationTypes.PeriodicTest; } }
    }
}
