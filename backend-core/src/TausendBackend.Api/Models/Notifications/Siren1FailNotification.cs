using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class Siren1FailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de sirena 1"; } }

        public override NotificationTypes Type { get { return NotificationTypes.Siren1Fail; } }
        protected override bool IsFail { get { return true; } }
    }
}
