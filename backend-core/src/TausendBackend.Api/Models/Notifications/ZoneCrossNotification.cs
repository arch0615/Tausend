using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class ZoneCrossNotification : NotificationBase
    {
        public override string Name { get { return "Cruce de zonas"; } }

        public override NotificationTypes Type { get { return NotificationTypes.ZoneCross; } }
    }
}
