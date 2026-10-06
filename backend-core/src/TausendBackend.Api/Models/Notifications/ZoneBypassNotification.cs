using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class ZoneBypassNotification : NotificationBase
    {
        public override string Name { get { return "Bypass de zonas"; } }
        public override NotificationTypes Type { get { return NotificationTypes.ZoneBypass; } }
        public override bool UseZoneParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Exclusión de zona " + Zone;
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
