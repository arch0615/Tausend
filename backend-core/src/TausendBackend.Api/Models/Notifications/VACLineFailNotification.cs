using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class VACLineFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de línea 220VAC"; } }

        public override NotificationTypes Type { get { return NotificationTypes.VACLineFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
