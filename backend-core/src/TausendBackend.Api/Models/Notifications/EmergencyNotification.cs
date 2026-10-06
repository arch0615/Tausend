using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class EmergencyNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Medical; } }
        public override string Name { get { return "Emergencia"; } }
        protected override string GetBody()
        {
            var result = "Se activó \"Emergencia\"";
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
