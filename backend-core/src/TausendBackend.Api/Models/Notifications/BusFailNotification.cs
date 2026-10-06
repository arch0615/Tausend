using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class BusFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de bus"; } }

        public override NotificationTypes Type { get { return NotificationTypes.BusFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
