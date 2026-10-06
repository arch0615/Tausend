using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class AutoArmCanceledNotification : NotificationBase
    {
        public override string Name { get { return ""; } }
        public override NotificationTypes Type { get { return NotificationTypes.AutoArmCanceled; } }
        public override bool UseUserParam { get { return true; } }
    }
}
