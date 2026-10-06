using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class InvalidFailNotification : NotificationBase
    {
        public override string Name { get { return "Falla inválida"; } }

        public override NotificationTypes Type { get { return NotificationTypes.InvalidFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
