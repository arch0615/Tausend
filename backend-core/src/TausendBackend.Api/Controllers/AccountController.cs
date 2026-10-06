using Microsoft.AspNetCore.Mvc;
using TausendBackend.Api.Business;
using TausendBackend.Api.Models;
using TausendBackend.Api.Requests;
using TausendBackend.Api.Responses;

namespace TausendBackend.Api.Controllers
{
    // Routes intentionally mirror the old WCF UriTemplates (/AccountService/{Op}) so any
    // existing client (dashboard, mobile) can point here with no URL changes.
    [ApiController]
    [Route("AccountService")]
    public class AccountController : ControllerBase
    {
        private readonly AccountBusiness _accountBusiness;

        public AccountController(AccountBusiness accountBusiness)
        {
            _accountBusiness = accountBusiness;
        }

        [HttpPost("CreateAccount")]
        public AccountCreatedResponse CreateAccount(Account account) => _accountBusiness.CreateAccount(account);

        [HttpPost("Login")]
        public AccountLoginResponse Login(Account account) => _accountBusiness.Login(account.Email ?? "", account.Password ?? "");

        [HttpPost("DeleteAccount")]
        public AccountDeletedResponse DeleteAccount(Account account)
        {
            if (!_accountBusiness.ValidateAccessToken(account.AccessToken ?? ""))
            {
                var res = new AccountDeletedResponse();
                res.InformUnauthorized();
                return res;
            }
            return _accountBusiness.DeleteAccount(account.AccessToken!);
        }

        [HttpPost("UpdateAccount")]
        public AccountCreatedResponse UpdateAccount(UpdateAccountRequest request)
        {
            if (!_accountBusiness.ValidateAccessToken(request.AccessToken))
            {
                var res = new AccountCreatedResponse();
                res.InformUnauthorized();
                return res;
            }
            return _accountBusiness.UpdateAccount(request.AccessToken, request.OldPassword, request.NewPassword);
        }

        [HttpPost("RecoverPassword")]
        public AccountCreatedResponse RecoverPassword(Account account) => _accountBusiness.UpdateAccountPassword(account.Email ?? "");

        [HttpPost("CreateAccountDeviceToken")]
        public AccountCreatedResponse CreateAccountDeviceToken(NewDeviceToken token)
        {
            if (!_accountBusiness.ValidateAccessToken(token.AccessToken ?? ""))
            {
                var res = new AccountCreatedResponse();
                res.InformUnauthorized();
                return res;
            }
            return _accountBusiness.CreateDeviceToken(token);
        }

        [HttpPost("ValidateAccessToken")]
        public AccountCreatedResponse ValidateAccessToken(AccessTokenRequest request)
        {
            var res = new AccountCreatedResponse();
            if (!_accountBusiness.ValidateAccessToken(request.AccessToken))
            {
                res.InformUnauthorized();
            }
            return res;
        }

        [HttpPost("Logout")]
        public AccountDeletedResponse Logout(DeviceTokenRequest request) => _accountBusiness.Logout(request.AccessToken, request.DeviceToken);

        [HttpPost("RefreshAccessToken")]
        public AccountLoginResponse RefreshAccessToken(RefreshTokenRequest request) => _accountBusiness.RefreshAccessToken(request.RefreshToken);

        [HttpPost("ResetPassword")]
        public AccountCreatedResponse ResetPassword(ResetPasswordRequest request) => _accountBusiness.ResetPassword(request.ResetToken, request.NewPassword);
    }
}
