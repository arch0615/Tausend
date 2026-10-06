using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class CommunicatorFail : NotificationBase
    {
        public override string Name { get { return "Falla de comunicador"; } }

        public override NotificationTypes Type { get { return NotificationTypes.CommunicatorFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
