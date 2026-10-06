using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class PhoneLineFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de línea telefónica"; } }

        public override NotificationTypes Type { get { return NotificationTypes.PhoneLineFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
