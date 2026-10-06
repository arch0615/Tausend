using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Tausend.Backend.Models;
using Tausend.Core.Business;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Services
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(Namespace = "http://Tausend.Wearelomo.com", InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class AccountService : IAccountService
    {
        public AccountCreatedResponse CreateAccount(Account account)
        {
            AccountBusiness bz = new AccountBusiness();
            return bz.CreateAccount(account);
        }

        public AccountCreatedResponse CreateAccountDeviceToken(NewDeviceToken token)
        {
            AccountBusiness bz = new AccountBusiness();
            if (!bz.ValidateAccessToken(token.AccessToken))
            {
                var res = new AccountCreatedResponse();
                res.InformUnauthorized();
                return res;
            }
            return bz.CreateDeviceToken(token);
        }

        public AccountDeletedResponse DeleteAccount(Account account)
        {
            AccountBusiness bz = new AccountBusiness();
            if (!bz.ValidateAccessToken(account.AccessToken))
            {
                var res = new AccountDeletedResponse();
                res.InformUnauthorized();
                return res;
            }
            return bz.DeleteAccount(account.AccessToken);
        }

        public AccountLoginResponse Login(Account account)
        {
            AccountBusiness bz = new AccountBusiness();
            return bz.Login(account.Email, account.Password);
        }

        public AccountDeletedResponse Logout(DeviceTokenRequest token)
        {
            AccountBusiness bz = new AccountBusiness();
            return bz.Logout(token.AccessToken, token.DeviceToken);
        }

        public AccountCreatedResponse RecoverPassword(Account updateRequest)
        {
            AccountBusiness bz = new AccountBusiness();
            return bz.UpdateAccountPassword(updateRequest.Email);
        }

        public AccountCreatedResponse UpdateAccount(UpdateAccountRequest updateRequest)
        {
            AccountBusiness bz = new AccountBusiness();
            if (!bz.ValidateAccessToken(updateRequest.AccessToken))
            {
                var res = new AccountCreatedResponse();
                res.InformUnauthorized();
                return res;
            }
            return bz.UpdateAccount(updateRequest.AccessToken, updateRequest.OldPassword, updateRequest.NewPassword);
        }

        public AccountCreatedResponse ValidateAccessToken(AccessTokenRequest token)
        {
            AccountBusiness bz = new AccountBusiness();
            var res = new AccountCreatedResponse();
            if (!bz.ValidateAccessToken(token.AccessToken))
            {
                res.InformUnauthorized();
            }
            return res;
        }

        public AccountLoginResponse RefreshAccessToken(RefreshTokenRequest request)
        {
            AccountBusiness bz = new AccountBusiness();
            return bz.RefreshAccessToken(request.RefreshToken);
        }

        public AccountCreatedResponse ResetPassword(ResetPasswordRequest request)
        {
            AccountBusiness bz = new AccountBusiness();
            return bz.ResetPassword(request.ResetToken, request.NewPassword);
        }
    }
}
