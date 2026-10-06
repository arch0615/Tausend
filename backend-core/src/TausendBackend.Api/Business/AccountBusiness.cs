using TausendBackend.Api.Dao;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Models;
using TausendBackend.Api.Responses;

namespace TausendBackend.Api.Business
{
    public class AccountBusiness
    {
        private readonly AccountDao _dao;
        private readonly EmailBusiness _emailBusiness;
        private readonly IConfiguration _configuration;

        public AccountBusiness(AccountDao dao, EmailBusiness emailBusiness, IConfiguration configuration)
        {
            _dao = dao;
            _emailBusiness = emailBusiness;
            _configuration = configuration;
        }

        public AccountCreatedResponse CreateAccount(Account account)
        {
            var response = new AccountCreatedResponse();
            try
            {
                var res = _dao.CreateAccount(account);
                if (res < 0)
                {
                    response.InformBusinessError("Ya existe una cuenta con el mail ingresado.", 1000);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public AccountCreatedResponse CreateDeviceToken(NewDeviceToken token)
        {
            var response = new AccountCreatedResponse();
            try
            {
                var res = _dao.CreateDeviceToken(token);
                if (res < 0)
                {
                    response.InformBusinessError("No se encontro la cuenta.", 1001);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public AccountLoginResponse Login(string email, string password)
        {
            var response = new AccountLoginResponse();
            try
            {
                var account = _dao.Login(email, password);
                if (account == null)
                {
                    response.InformWrongData("Login erróneo. El usuario o la contraseña no son correctos.", 20000);
                }
                else
                {
                    // NOTE: the original also re-validated each linked device's PIN here
                    // (DeviceBusiness.ValidatePinOnDevice) and dropped/offlined stale links.
                    // That's Device-domain logic depending on the still-unresolved relay
                    // question, so it's intentionally not ported yet -- Devices is returned
                    // as-is from the DB rather than reconciled against a live panel.
                    response.SetAccount(account);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public AccountLoginResponse RefreshAccessToken(string refreshToken)
        {
            var response = new AccountLoginResponse();
            try
            {
                var account = _dao.RefreshAccessToken(refreshToken);
                if (account == null)
                {
                    response.InformUnauthorized();
                }
                else
                {
                    response.SetAccount(account);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public AccountDeletedResponse DeleteAccount(string accessToken)
        {
            var response = new AccountDeletedResponse();
            try
            {
                _dao.DeleteAccount(accessToken);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public AccountDeletedResponse Logout(string accessToken, string deviceToken)
        {
            var response = new AccountDeletedResponse();
            try
            {
                _dao.Logout(accessToken, deviceToken);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public Account? GetAccount(string accessToken)
        {
            try
            {
                return _dao.GetAccount(accessToken);
            }
            catch
            {
                return null;
            }
        }

        public AccountCreatedResponse UpdateAccount(string accessToken, string oldPassword, string newPassword)
        {
            var response = new AccountCreatedResponse();
            try
            {
                var res = _dao.UpdateAccount(accessToken, oldPassword, newPassword);
                if (res == -1)
                {
                    response.InformWrongData("La cuenta no existe.", 3000);
                }
                else if (res == -2)
                {
                    response.InformWrongData("La contraseña antigua es inválida.", 3001);
                }
                else if (res <= 0)
                {
                    response.InformServerError("Error no especificado");
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public AccountCreatedResponse UpdateAccountPassword(string email)
        {
            // Always reports success, whether or not the email matches an account,
            // so this endpoint can't be used to enumerate registered addresses.
            var response = new AccountCreatedResponse();
            try
            {
                var tokenInfo = _dao.CreatePasswordResetToken(email);
                if (tokenInfo != null)
                {
                    var baseUrl = _configuration["PasswordResetUrl"];
                    var resetLink = baseUrl + "?token=" + Uri.EscapeDataString(tokenInfo.ResetToken);
                    _emailBusiness.SendPasswordResetEmail(tokenInfo.Email, tokenInfo.FirstName, resetLink);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public AccountCreatedResponse ResetPassword(string resetToken, string newPassword)
        {
            var response = new AccountCreatedResponse();
            try
            {
                var res = _dao.ResetPasswordWithToken(resetToken, newPassword);
                if (res <= 0)
                {
                    response.InformWrongData("El enlace de recuperación es inválido o ha expirado.", 3002);
                }
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public List<Account> EnumOwnersOfDevice(string identifier) => _dao.EnumOwnersOfDevice(identifier);

        public List<DeviceToken> EnumDeviceTokenOfAccount(long accountId) => _dao.EnumDeviceTokenOfAccount(accountId);

        public bool ValidateAccessToken(string accessToken) => ResolveAccountId(accessToken) > 0;

        /// <summary>
        /// The account id behind a token, or 0 if the token is missing/malformed/expired.
        /// Use this (not GetAccount) when the caller's own account id is needed for an
        /// ownership check -- dbo.ValidateAccessToken enforces ExpirationDateTime; dbo.GetAccount
        /// does not.
        /// </summary>
        public long ResolveAccountId(string accessToken)
        {
            var accountId = _dao.ValidateAccessToken(accessToken);
            return accountId > 0 ? accountId : 0;
        }

        /// <summary>Valid token AND the account's Role is Admin -- used to gate the admin/fleet endpoints.</summary>
        public bool IsAdmin(string accessToken)
        {
            var account = _dao.GetAccount(accessToken);
            return account != null && account.Role == AccountRole.Admin;
        }

        /// <summary>Valid token AND the account's Role is Installer or Admin -- gates Installer mode's
        /// raw command endpoint (CommandController.SendInstallerCommand). EndUser accounts are
        /// refused server-side, not just hidden client-side.</summary>
        public bool IsInstallerOrAdmin(string accessToken)
        {
            var account = _dao.GetAccount(accessToken);
            return account != null && (account.Role == AccountRole.Installer || account.Role == AccountRole.Admin);
        }
    }
}
