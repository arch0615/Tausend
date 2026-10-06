using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Entities.Models
{
    public class Event
    {
        public long EventId { get; set; }
        public int Secuence { get; set; }
        public DateTime EventDateTime { get; set; }
        public string EventType { get; set; }
        public int NotificationType { get; set; }
        public int Partition { get; set; }
        public int AlarmParameter { get; set; }
        public string AlarmIdentifier { get; set; }
        public string Text { get; set; }
        public string StringDate { get; set; }
    }
}