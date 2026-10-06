using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class ListOfZonesResponse : BaseResponse
    {
        [DataMember]
        public List<Zone> Zones { get; private set; }

        public ListOfZonesResponse(List<Zone> zones)
        {
            Zones = zones;
        }

    }
}