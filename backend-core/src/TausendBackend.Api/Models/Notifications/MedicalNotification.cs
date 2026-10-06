using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class MedicalNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.Medical; } }
        public override string Name { get { return "Emergencia"; } }
        //public override string Name { get { return "Emergencia médica"; } }
        public override bool UseDualParam { get { return true; } }
        public override bool UseZoneParam { get { return true; } }
        public override bool UseKeypadParam { get { return true; } }
        protected override string GetBody()
        {
            var result = "Se activó \"Emergencia\"";
            return string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }
    }
}
