using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class UnknownEventNotification : NotificationBase
    {
        public override string Name { get { return "Evento desconocido"; } }

        public override NotificationTypes Type { get { return NotificationTypes.UnknownEvent; } }
    }
}
