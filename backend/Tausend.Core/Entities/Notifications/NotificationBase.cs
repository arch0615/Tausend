using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public abstract class NotificationBase
    {
        public abstract string Name { get; }
        public string DeviceToken { get; set; }
        public PhoneOS OS { get; set; }
        public string Icon { get; set; }
        public string Sound { get; set; }
        public abstract NotificationTypes Type { get; }
        public string Body { get { return GetBody(); } }
        public string Date { get; set; }
        public virtual string Title { get { return Name; } }
        public string EventType { get; set; }
        public int Zone { get; set; }
        public int Keypad { get; set; }
        public int User { get; set; }
        public string UserName { get; set; }
        public int Point { get; set; }
        public virtual bool UseZoneParam { get { return false; } }
        public virtual bool UseKeypadParam { get { return false; } }
        public virtual bool UseUserParam { get { return false; } }
        public virtual bool UsePointParam { get { return false; } }
        public virtual bool UseDualParam { get { return false; } }
        protected virtual bool IsFail { get { return false; } }
        public string Email { get; set; }
        public string DeviceDescription { get; set; }

        protected bool IsEvent()
        {
            return EventType.Equals("E");
        }

        protected bool IsReplacement()
        {
            return EventType.Equals("R");
        }

        protected virtual string GetBody()
        {
            string result;
            if (IsFail)
            {
                result = GetBodyForFails();
            }
            else if (UseDualParam)
            {
                result = GetBodyForDualParam();
            }
            else
            {
                result = GetBodyForOneParam();
            }
            return String.Format("[{0}] ({2}): {1}", Date, result, DeviceDescription);
        }

        private string GetBodyForFails()
        {
            if (IsEvent())
            {
                return Name;
            }
            else if (IsReplacement())
            {
                return String.Format("Reposición de {0}", Name);
            }
            return String.Format("Se activó la alarma {0}", Name);
        }

        private string GetBodyForDualParam()
        {
            if (UseZoneParam && Zone > 0 && Zone != 999)
            {
                return String.Format("Se activó la alarma {0} en la zona {1}", Name, Zone);
            }
            else if (UseKeypadParam && Keypad > 0 && Keypad != 999)
            {
                return String.Format("Se activó la alarma {0} mediante el teclado {1}", Name, Keypad);
            }
            else if (UseUserParam)
            {
                return String.Format("{0} activó la alarma {1}", UserName, Name);
            }
            return String.Format("Se activó la alarma {0}", Name);
        }

        private string GetBodyForOneParam()
        {
            if (UseZoneParam && Zone != 999)
            {
                if (IsReplacement())
                {
                    return String.Format("Reposición de {0} zona {1}", Name, Zone);
                }
                else
                {
                    return String.Format("Se activó la alarma {0} en la zona {1}", Name, Zone);
                }
            }
            else if (UseKeypadParam && Keypad != 999)
            {
                return String.Format("Se activó la alarma {0} mediante el teclado {1}", Name, Keypad);
            }
            else if (UseUserParam)
            {
                return String.Format("{0} activó la alarma {1}", UserName, Name);
            }
            else if (UsePointParam)
            {
                return String.Format("Se activó la alarma {0} en el punto {1}", Name, Point);
            }
            return String.Format("Se activó la alarma {0}", Name);
        }
    }
}