using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class PersonalMedicalNotification : NotificationBase
    {
        public override NotificationTypes Type { get { return NotificationTypes.PersonalMedical; } }
        public override string Name { get { return "Emergencia"; } }
        //public override string Name { get { return "Emergencia médica personal"; } }
        public override bool UseUserParam { get { return true; } }
    }
}