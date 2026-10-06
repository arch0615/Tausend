namespace TausendBackend.Api.Models
{
    public class ProgramControl
    {
        public long ProgramControlId { get; set; }
        public long DeviceId { get; set; }
        public int ProgramControlNumber { get; set; }
        public string? Name { get; set; }
        public bool Activated { get; set; }
    }
}
