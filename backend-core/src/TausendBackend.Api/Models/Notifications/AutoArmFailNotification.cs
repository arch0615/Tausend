using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class AutoArmFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de armado automático"; } }
        public override NotificationTypes Type { get { return NotificationTypes.AutoArmFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
