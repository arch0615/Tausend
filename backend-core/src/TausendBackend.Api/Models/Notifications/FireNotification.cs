using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class FireNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Fire; } }
        public override string Name { get { return "Fuego"; } }
        public override bool UseDualParam { get { return true; } }
        public override bool UseZoneParam { get { return true; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}
