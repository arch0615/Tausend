using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Entities.Notifications
{
    public class Auxiliar12VFail : NotificationBase
    {
        public override string Name { get { return "Falla 12V auxiliar"; } }

        public override NotificationTypes Type { get { return NotificationTypes.Auxiliar12VFail; } }
        protected override bool IsFail { get { return true; } }
    }
}