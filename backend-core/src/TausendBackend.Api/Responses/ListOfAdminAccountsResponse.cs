using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class ListOfAdminAccountsResponse : BaseResponse
    {
        public List<AdminAccountSummary>? Accounts { get; set; }

        public ListOfAdminAccountsResponse(List<AdminAccountSummary>? accounts)
        {
            Accounts = accounts;
        }
    }
}
