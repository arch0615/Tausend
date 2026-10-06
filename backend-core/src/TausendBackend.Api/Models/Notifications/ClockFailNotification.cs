using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class ClockFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de reloj"; } }

        public override NotificationTypes Type { get { return NotificationTypes.ClockFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
