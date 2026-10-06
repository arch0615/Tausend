using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class FailStatusResponse : BaseResponse
    {
        [DataMember]
        public bool AC { get; private set; }
        [DataMember]
        public bool BAT { get; private set; }
        [DataMember]
        public bool TLM { get; private set; }
        [DataMember]
        public bool BELL1 { get; private set; }
        [DataMember]
        public bool VAUX { get; private set; }
        [DataMember]
        public bool CLOCK { get; private set; }
        [DataMember]
        public bool CEL { get; private set; }
        [DataMember]
        public bool COMU { get; private set; }
        [DataMember]
        public bool BUS { get; private set; }
        [DataMember]
        public bool BELL2 { get; private set; }
        public FailStatusResponse(string text) : base()
        {
            if (text.Contains("AC"))
                AC = true;
            if (text.Contains("BAT"))
                BAT = true;
            if (text.Contains("TLM"))
                TLM = true;
            if (text.Contains("BELL-1"))
                BELL1 = true;
            if (text.Contains("VAUX"))
                VAUX = true;
            if (text.Contains("CLOCK"))
                CLOCK = true;
            if (text.Contains("CEL"))
                CEL = true;
            if (text.Contains("COMU"))
                COMU = true;
            if (text.Contains("BUS"))
                BUS = true;
            if (text.Contains("BELL-2"))
                BELL2 = true;
        }
    }
}