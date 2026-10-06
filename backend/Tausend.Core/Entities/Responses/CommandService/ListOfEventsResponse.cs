using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class ListOfEventsResponse : BaseResponse
    {
        [DataMember]
        public List<Event> Events { get; private set; }

        public ListOfEventsResponse(List<Event> events)
        {
            Events = events;
        }
    }
}