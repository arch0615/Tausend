using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class ListOfExclusionsResponse : BaseResponse
    {
        [DataMember]
        public List<Exclusion> Exclusions { get; private set; }

        public ListOfExclusionsResponse(List<Exclusion> exclusions)
        {
            Exclusions = exclusions;
        }
    }
}