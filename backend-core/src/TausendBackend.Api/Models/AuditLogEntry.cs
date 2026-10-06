namespace TausendBackend.Api.Models
{
    public class AuditLogEntry
    {
        public long AuditLogId { get; set; }
        public long ActorAccountId { get; set; }
        public string? ActorEmail { get; set; }
        public string? ActorFirstName { get; set; }
        public string? ActorLastName { get; set; }
        public string Action { get; set; } = "";
        public string TargetType { get; set; } = "";
        public long? TargetId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
