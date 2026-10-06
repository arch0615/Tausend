namespace TausendBackend.Api.Responses
{
    public class BatteryStateResponse : BaseResponse
    {
        public float InTension { get; set; }
        public float ChargeTension { get; set; }
        public float Current { get; set; }
        public float TestTension { get; set; }
    }
}
