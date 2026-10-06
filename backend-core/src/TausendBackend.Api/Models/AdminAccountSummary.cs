using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Models
{
    public class AdminAccountSummary
    {
        public long AccountId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public AccountRole Role { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? LastLoginDateTime { get; set; }
    }
}
