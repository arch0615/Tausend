using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class RecentArmedAlarmNotification : NotificationBase
    {
        public override string Name { get { return "Alarma con armado reciente"; } }

        public override NotificationTypes Type { get { return NotificationTypes.RecentArmedAlarm; } }

        protected override string GetBody()
        {
            var result = "Se activo \"Alarma con armado reciente\"";
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
