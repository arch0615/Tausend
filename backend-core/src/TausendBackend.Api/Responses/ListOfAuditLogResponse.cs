using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfAuditLogResponse : BaseResponse
    {
        public List<AuditLogEntry>? Entries { get; set; }

        public ListOfAuditLogResponse(List<AuditLogEntry>? entries)
        {
            Entries = entries;
        }
    }
}
