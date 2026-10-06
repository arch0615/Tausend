namespace TausendBackend.Api.Requests
{
    public class CreateScheduledPgmActionRequest
    {
        public string AccessToken { get; set; } = "";
        public long DeviceId { get; set; }
        public int ProgramControlNumber { get; set; }
        public TimeSpan TimeOfDay { get; set; }
        public byte DaysOfWeekMask { get; set; }
        public bool DesiredState { get; set; }
    }
}
