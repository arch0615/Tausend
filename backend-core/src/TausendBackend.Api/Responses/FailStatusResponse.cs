namespace TausendBackend.Api.Responses
{
    public class FailStatusResponse : BaseResponse
    {
        public bool AC { get; private set; }
        public bool BAT { get; private set; }
        public bool TLM { get; private set; }
        public bool BELL1 { get; private set; }
        public bool VAUX { get; private set; }
        public bool CLOCK { get; private set; }
        public bool CEL { get; private set; }
        public bool COMU { get; private set; }
        public bool BUS { get; private set; }
        public bool BELL2 { get; private set; }

        public FailStatusResponse(string text)
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
