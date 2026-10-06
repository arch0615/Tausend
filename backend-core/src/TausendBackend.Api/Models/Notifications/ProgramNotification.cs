using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class ProgramNotification : NotificationBase
    {
        public override string Name { get { return "Programación"; } }
        public override NotificationTypes Type { get { return NotificationTypes.Program; } }
        public override bool UseUserParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Programación: " + User.ToString();
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
