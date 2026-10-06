using TausendBackend.Api.Models;

namespace TausendBackend.Api.Responses
{
    public class AccountLoginResponse : BaseResponse
    {
        public Account? Account { get; set; }
        public bool PinChanged { get; set; }

        public AccountLoginResponse()
        {
            Account = null;
            PinChanged = false;
            InformServerError("No account selected for return");
        }

        public void SetAccount(Account account)
        {
            InformOk();
            Account = account;
        }
    }
}
