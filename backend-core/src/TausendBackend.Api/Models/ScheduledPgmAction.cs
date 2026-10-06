namespace TausendBackend.Api.Models
{
    public class ScheduledPgmAction
    {
        public long ScheduledPgmActionId { get; set; }
        public long DeviceId { get; set; }
        public int ProgramControlNumber { get; set; }
        public TimeSpan TimeOfDay { get; set; }
        public byte DaysOfWeekMask { get; set; }
        public bool DesiredState { get; set; }
        public bool Enabled { get; set; }
    }
}
