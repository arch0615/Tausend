using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class MemoryDisarmedNotification : NotificationBase
    {
        public override string Name { get { return "Desarmado con memorias"; } }

        public override NotificationTypes Type { get { return NotificationTypes.MemoryDisarmed; } }
    }
}
