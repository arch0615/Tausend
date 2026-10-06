using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Entities.Requests
{
    public class NotificationRequest
    {
        public int Secuence { get; set; }
        public string EventDateTime { get; set; }
        public string EventType { get; set; }
        public int NotificationType { get; set; }
        public int Partition { get; set; }
        public int AlarmParameter { get; set; }
        public string AlarmIdentifier { get; set; }
    }
}