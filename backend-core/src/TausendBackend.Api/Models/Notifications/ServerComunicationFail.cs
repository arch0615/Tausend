using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class ServerComunicationFail : NotificationBase
    {
        public override string Name { get { return "Falla de conexión con el servidor"; } }
        public override NotificationTypes Type { get { return NotificationTypes.ServerComunicationFail; } }
        protected override bool IsFail { get { return true; } }
    }
}
