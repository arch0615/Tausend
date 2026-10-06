using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class BatteryFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de batería"; } }

        public override NotificationTypes Type { get { return NotificationTypes.BatteryFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
