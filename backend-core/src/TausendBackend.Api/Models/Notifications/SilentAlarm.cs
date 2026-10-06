using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class SilentAlarm : NotificationBase
    {
        public override string Name { get { return "Alarma Silenciosa"; } }
        public override NotificationTypes Type { get { return NotificationTypes.SilentAlarm; } }
        public override bool UseZoneParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Se activo \"Asalto Silencioso\"";
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
