namespace TausendBackend.Api.Responses
{
    public class TimeResponse : BaseResponse
    {
        public DateTime DeviceTime { get; set; }
        public DateTime ServerTime { get; set; }
    }
}
