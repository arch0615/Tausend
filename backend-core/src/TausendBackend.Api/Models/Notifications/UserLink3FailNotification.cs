using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class UserLink3FailNotification : NotificationBase
    {
        public override string Name { get { return "Falla de vinculación"; } }
        public override NotificationTypes Type { get { return NotificationTypes.UserLink3Fail; } }
        public override bool UseUserParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "La vinculación del usuario ha fallado 3 veces.";
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
