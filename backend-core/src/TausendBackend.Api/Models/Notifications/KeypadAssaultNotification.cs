using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class KeypadAssaultNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.KeypadAssault; } }
        public override string Name { get { return "Asalto por teclado"; } }
        public override bool UseKeypadParam { get { return true; } }
    }
}
