using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Entities
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class ListOfProgramControlResponse : BaseResponse
    {
        [DataMember]
        public List<ProgramControl> ProgramControls { get; private set; }

        public ListOfProgramControlResponse(List<ProgramControl> programControls)
        {
            ProgramControls = programControls;
        }

    }
}