using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class Auxiliar12VFail : NotificationBase
    {
        public override string Name { get { return "Falla 12V auxiliar"; } }

        public override NotificationTypes Type { get { return NotificationTypes.Auxiliar12VFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
