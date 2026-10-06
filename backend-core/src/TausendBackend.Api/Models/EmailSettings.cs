namespace TausendBackend.Api.Models
{
    public class EmailSettings
    {
        public string? SenderEmail { get; set; }
        public string? SenderName { get; set; }
        public string? SendGridApiKey { get; set; }
    }
}
