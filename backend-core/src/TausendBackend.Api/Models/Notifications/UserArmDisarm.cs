using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models.Notifications
{
    public class UserArmDisarm : NotificationBase
    {
        public override string Name { get { return GetName(); } }
        public override NotificationTypes Type { get { return NotificationTypes.UserArmDisarm; } }
        public override bool UseUserParam { get { return true; } }

        private string GetName()
        {
            if (IsReplacement())
            {
                return "Armado por usuario";
            }
            else if (IsEvent())
            {
                return "Desarmado por usuario";
            }
            else
            {
                return "Armado/Desarmado por usuario";
            }
        }

        protected override string GetBody()
        {
            string result;
            if (IsReplacement())
            {
                result = string.Format("{0} armó la alarma", UserName);
                result = string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
            }
            else if (IsEvent())
            {
                result = string.Format("{0} desarmó la alarma", UserName);
                result = string.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
            }
            else
            {
                result = base.GetBody();
            }
            return result;
        }
    }
}
