using TausendBackend.Api.Models;

namespace TausendBackend.Api.Requests
{
    // NOTE: filename matches the original (Tausend.Core.Entities.Requests.CreateProgramControlRequest.cs),
    // but the class inside was actually named PostProgramControlRequest in the source backend --
    // kept as-is here so callers built elsewhere (ProgramControl-related consumers) match it exactly.
    public class PostProgramControlRequest
    {
        public long DeviceId { get; set; }
        public string AccessToken { get; set; } = "";
        public List<ProgramControl> ProgramControls { get; set; } = new();
    }
}
