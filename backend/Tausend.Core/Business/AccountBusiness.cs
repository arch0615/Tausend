using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Tausend.Backend.Models;
using Tausend.Core.Dao;
using Tausend.Core.Enums;
using Tausend.Core.Models;
using Tausend.Core.Responses;
using Tausend.Backend.Business;

namespace Tausend.Core.Business
{
    public class AccountBusiness
    {
        private AccountDao dao;
        public AccountBusiness()
        {
            dao = new AccountDao();
        }

        public AccountCreatedResponse CreateAccount(Account account)
        {
            var response = new AccountCreatedResponse();
            try
            {
                var res = dao.CreateAccount(account);
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
                var res = dao.CreateDeviceToken(token);
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

        public AccountLoginResponse Login(String email, String password)
        {
            var response = new AccountLoginResponse();
            var dvbz = new DeviceBusiness();
            try
            {
                var account = dao.Login(email, password);
                if (account == null)
                {
                    response.InformWrongData("Login erróneo. El usuario o la contraseña no son correctos.", 20000);
                }
                else
                {
                    account.Devices.ForEach(device =>
                    {
                        // Validates if PIN exist in Device using device's identifier (mac)
                        string pinValidation = dvbz.ValidatePinOnDevice(device.Mac, device.Pin);
                        if (device.DeviceId != 0)
                        {
                            switch (pinValidation)
                            {
                                case "ok": break; // no action needed
                                case "invalid": // if invalid, remove device from user
                                    response.PinChanged = true;
                                    account.RemovedDevices.Add(device);
                                    // Removes device from list searching for mac (identifier)
                                    account.Devices = account.Devices.Where(d => d.Mac != device.Mac).ToList();

                                    // Deletes account-device relationship from DB
                                    dao.DeleteRelationship(account.AccountId, device.DeviceId);

                                    break;
                                case "offline": // if online, add to offline list
                                    account.OfflineDevices.Add(device);
                                    break;
                            }
                        }
                    });

                    if (account.Devices.Count == 0)
                    {
                        AccountDevice ad = new AccountDevice()
                        {
                            Description = "",
                            Mac = "",
                            Pin = "",
                            DeviceId = 0
                        };
                        account.Devices.Add(ad);
                    }

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
                var account = dao.RefreshAccessToken(refreshToken);
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
                dao.DeleteAccount(accessToken);
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
                dao.Logout(accessToken, deviceToken);
            }
            catch (Exception e)
            {
                response.InformServerError(e.Message);
            }
            return response;
        }

        public Account GetAccount(string accessToken)
        {
            Account response = null;
            try
            {
                response = dao.GetAccount(accessToken);
            }
            catch (Exception e)
            {
            }
            return response;
        }

        public AccountCreatedResponse UpdateAccount(string accessToken, string oldPassword, string newPassword)
        {
            var response = new AccountCreatedResponse();
            try
            {
                var res = dao.UpdateAccount(accessToken, oldPassword, newPassword);
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
                var tokenInfo = dao.CreatePasswordResetToken(email);
                if (tokenInfo != null)
                {
                    var baseUrl = ConfigurationManager.AppSettings["PasswordResetUrl"];
                    var resetLink = baseUrl + "?token=" + Uri.EscapeDataString(tokenInfo.ResetToken);
                    EmailBusiness emailBz = new EmailBusiness();
                    emailBz.SendPasswordResetEmail(tokenInfo.Email, tokenInfo.FirstName, resetLink);
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
                var res = dao.ResetPasswordWithToken(resetToken, newPassword);
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

        public List<Account> EnumOwnersOfDevice(string identifier)
        {
            var res = dao.EnumOwnersOfDevice(identifier);
            return res;
        }

        public List<DeviceToken> EnumDeviceTokenOfAccount(long accountId)
        {
            var res = dao.EnumDeviceTokenOfAccount(accountId);
            return res;
        }

        public bool ValidateAccessToken(string accessToken)
        {
            return ResolveAccountId(accessToken) > 0;
        }

        /// <summary>
        /// The account id behind a token, or 0 if the token is missing/malformed/expired.
        /// Use this (not GetAccount) when the caller's own account id is needed for an
        /// ownership check -- dbo.ValidateAccessToken enforces ExpirationDateTime; dbo.GetAccount
        /// does not.
        /// </summary>
        public long ResolveAccountId(string accessToken)
        {
            var accountId = dao.ValidateAccessToken(accessToken);
            return accountId > 0 ? accountId : 0;
        }

        /// <summary>Valid token AND the account's Role is Admin -- used to gate the admin/fleet endpoints.</summary>
        public bool IsAdmin(string accessToken)
        {
            var account = dao.GetAccount(accessToken);
            return account != null && account.Role == AccountRole.Admin;
        }

        /// <summary>Valid token AND the account's Role is Installer or Admin -- gates Installer mode's
        /// raw command endpoint (CommandService.SendInstallerCommand). EndUser accounts are refused
        /// server-side, not just hidden client-side.</summary>
        public bool IsInstallerOrAdmin(string accessToken)
        {
            var account = dao.GetAccount(accessToken);
            return account != null && (account.Role == AccountRole.Installer || account.Role == AccountRole.Admin);
        }
    }
}