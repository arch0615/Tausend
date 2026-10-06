using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class BellDisarmNotification : NotificationBase
    {
        public override string Name { get { return "Desarmado con alarma sonando"; } }

        public override NotificationTypes Type { get { return NotificationTypes.BellDisarm; } }

        protected override string GetBody()
        {
            var result = "Se desarmó la alarma mientras se encontraba sonando";
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
