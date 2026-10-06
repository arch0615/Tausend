using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Backend.Models;
using Tausend.Core.Enums;
using Tausend.Core.Models;
using Tausend.Core.Security;

namespace Tausend.Core.Dao
{
    public class AccountDao : BaseDao
    {
        public long CreateAccount(Account account)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var passwordHash = PasswordHasher.HashPassword(account.Password);
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.CreateAccount");
                dBContext.AddStringParameter(sqlcommand, "FirstName", account.FirstName);
                dBContext.AddStringParameter(sqlcommand, "LastName", account.LastName);
                dBContext.AddStringParameter(sqlcommand, "EMail", account.Email);
                dBContext.AddStringParameter(sqlcommand, "PasswordHash", passwordHash);
                var x = sqlcommand.ExecuteScalar();
                res = (long)x;
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public long ValidateAccessToken(string accessToken)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.ValidateAccessToken");
                dBContext.AddStringParameter(sqlcommand, "AccessToken", accessToken);
                res = (long)sqlcommand.ExecuteScalar();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public Account Login(String email, String password)
        {
            dBContext.OpenConnection();
            Account res = null;
            try
            {
                var credentials = GetLoginCredentials(email);
                if (credentials != null && credentials.Enabled && VerifyPassword(password, credentials))
                {
                    var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.CreateLoginSession");
                    dBContext.AddLongParameter(sqlcommand, "AccountId", credentials.AccountId);
                    using (var reader = sqlcommand.ExecuteReader())
                    {
                        res = GetAccountFromReader(reader);
                    }
                }
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        /// <summary>Raw credential row for an account, used only to verify a password in C#.</summary>
        private class LoginCredentials
        {
            public long AccountId;
            public string PasswordHash;
            public byte[] LegacyPassword;
            public bool Enabled;
        }

        private LoginCredentials GetLoginCredentials(string email)
        {
            var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.GetLoginCredentials");
            dBContext.AddStringParameter(sqlcommand, "Email", email);
            using (var reader = sqlcommand.ExecuteReader())
            {
                return ReadLoginCredentials(reader);
            }
        }

        private LoginCredentials GetAccountCredentialsByToken(string accessToken)
        {
            var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.GetAccountCredentialsByToken");
            dBContext.AddStringParameter(sqlcommand, "AccessToken", accessToken);
            using (var reader = sqlcommand.ExecuteReader())
            {
                return ReadLoginCredentials(reader);
            }
        }

        private LoginCredentials ReadLoginCredentials(SqlDataReader reader)
        {
            if (!reader.Read())
                return null;
            return new LoginCredentials
            {
                AccountId = reader.GetInt64(0),
                PasswordHash = reader.IsDBNull(1) ? null : reader.GetString(1),
                LegacyPassword = reader.IsDBNull(2) ? null : (byte[])reader[2],
                Enabled = !reader.IsDBNull(3) && reader.GetBoolean(3)
            };
        }

        /// <summary>
        /// Verifies against the bcrypt hash if the account has one; otherwise falls back to
        /// the legacy SHA-256 hash and, if that matches, silently upgrades the account to
        /// bcrypt so the weak hash stops being used from this point on.
        /// </summary>
        private bool VerifyPassword(string plainTextPassword, LoginCredentials credentials)
        {
            if (!string.IsNullOrEmpty(credentials.PasswordHash))
            {
                return PasswordHasher.VerifyBcrypt(plainTextPassword, credentials.PasswordHash);
            }
            if (PasswordHasher.VerifyLegacySha256(plainTextPassword, credentials.LegacyPassword))
            {
                SetPasswordHash(credentials.AccountId, PasswordHasher.HashPassword(plainTextPassword));
                return true;
            }
            return false;
        }

        private void SetPasswordHash(long accountId, string passwordHash)
        {
            var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.SetPasswordHash");
            dBContext.AddLongParameter(sqlcommand, "AccountId", accountId);
            dBContext.AddStringParameter(sqlcommand, "PasswordHash", passwordHash);
            sqlcommand.ExecuteNonQuery();
        }

        public Account RefreshAccessToken(string refreshToken)
        {
            dBContext.OpenConnection();
            Account res = null;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.RefreshAccessToken");
                dBContext.AddStringParameter(sqlcommand, "RefreshToken", refreshToken);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var accountId = reader.GetInt64(0);
                        if (accountId > 0)
                        {
                            res = new Account();
                            res.AccountId = accountId;
                            res.AccessToken = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            res.RefreshToken = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        }
                    }
                }
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public Account GetAccount(string accessToken)
        {
            dBContext.OpenConnection();
            Account res = null;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.GetAccount");
                dBContext.AddStringParameter(sqlcommand, "AccessToken", accessToken);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    res = GetAccountFromReader(reader);
                }
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public void DeleteAccount(string token)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.DeleteAccount");
                dBContext.AddStringParameter(sqlCommand, "AccessToken", token);
                sqlCommand.ExecuteNonQuery();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
        }

        public void Logout(string accessToken, string deviceToken)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.Logout");
                dBContext.AddStringParameter(sqlCommand, "AccessToken", accessToken);
                dBContext.AddStringParameter(sqlCommand, "DeviceToken", deviceToken);
                sqlCommand.ExecuteNonQuery();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
        }

        private Account GetAccountFromReader(SqlDataReader reader)
        {
            var res = new Account();
            while (reader.Read())
            {
                res.AccountId = reader.GetInt64(0);
                if (res.AccountId > 0)
                {
                    res.Email = reader.GetString(1);
                    res.FirstName = reader.GetString(2);
                    res.LastName = reader.GetString(3);
                    res.AccessToken = reader.GetString(4);
                    AccountDevice ad = new AccountDevice();
                    ad.Description = reader.IsDBNull(5) ? "" : reader.GetString(5);
                    ad.Mac = reader.IsDBNull(6) ? "" : reader.GetString(6);
                    ad.Pin = reader.IsDBNull(7) ? "" : reader.GetString(7);
                    ad.DeviceId = reader.IsDBNull(8) ? 0 : reader.GetInt64(8);
                    res.Devices.Add(ad);
                    res.Role = (AccountRole)reader.GetByte(9);
                }
                else
                {
                    res = null;
                }
            }
            if (res != null && reader.NextResult())
            {
                while (reader.Read())
                {
                    AccountSmsDevice device = new AccountSmsDevice()
                    {
                        DeviceId = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                        Description = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Identifier = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        DevicePin = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        SimPin = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        DeviceType = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    };
                    res.SmsDevices.Add(device);
                }
            }
            // CreateLoginSession returns a third result set with the session's refresh token; GetAccount does not.
            if (res != null && reader.NextResult() && reader.Read() && !reader.IsDBNull(0))
            {
                res.RefreshToken = reader.GetString(0);
            }
            return res;
        }

        /// <summary>Returns the account id on success, -1 if the token doesn't resolve to an account, -2 if oldPassword is wrong.</summary>
        public long UpdateAccount(string accessToken, string oldPassword, string newPassword)
        {
            long res;
            dBContext.OpenConnection();
            try
            {
                var credentials = GetAccountCredentialsByToken(accessToken);
                if (credentials == null || !credentials.Enabled)
                {
                    res = -1;
                }
                else if (!VerifyPassword(oldPassword, credentials))
                {
                    res = -2;
                }
                else
                {
                    var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.ChangePassword");
                    dBContext.AddLongParameter(sqlCommand, "AccountId", credentials.AccountId);
                    dBContext.AddStringParameter(sqlCommand, "PasswordHash", PasswordHasher.HashPassword(newPassword));
                    sqlCommand.ExecuteNonQuery();
                    res = credentials.AccountId;
                }
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public class PasswordResetTokenInfo
        {
            public long AccountId;
            public string ResetToken;
            public string Email;
            public string FirstName;
        }

        /// <summary>Returns null if the email doesn't match an enabled account (caller should still report success to avoid leaking account existence).</summary>
        public PasswordResetTokenInfo CreatePasswordResetToken(string email)
        {
            dBContext.OpenConnection();
            PasswordResetTokenInfo res = null;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreatePasswordResetToken");
                dBContext.AddStringParameter(sqlCommand, "Email", email);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var accountId = reader.GetInt64(0);
                        if (accountId > 0)
                        {
                            res = new PasswordResetTokenInfo
                            {
                                AccountId = accountId,
                                ResetToken = reader.GetString(1),
                                Email = reader.GetString(2),
                                FirstName = reader.GetString(3)
                            };
                        }
                    }
                }
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        /// <summary>Returns the account id on success, -1 if the token is missing/expired/already used.</summary>
        public long ResetPasswordWithToken(string resetToken, string newPassword)
        {
            long res = -1;
            dBContext.OpenConnection();
            try
            {
                var passwordHash = PasswordHasher.HashPassword(newPassword);
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.ResetPasswordWithToken");
                dBContext.AddStringParameter(sqlCommand, "ResetToken", resetToken);
                dBContext.AddStringParameter(sqlCommand, "NewPasswordHash", passwordHash);
                res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public long CreateDeviceToken(NewDeviceToken token)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.CreateDeviceToken");
                dBContext.AddStringParameter(sqlcommand, "AccessToken", token.AccessToken);
                dBContext.AddStringParameter(sqlcommand, "OS", token.OS);
                dBContext.AddStringParameter(sqlcommand, "DeviceToken", token.Token);
                var x = sqlcommand.ExecuteScalar();
                res = (long)x;
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public List<Account> EnumOwnersOfDevice(string identifier)
        {
            dBContext.OpenConnection();
            var res = new List<Account>();
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.EnumOwnersOfDevice");
                dBContext.AddStringParameter(sqlcommand, "Identifier", identifier);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    res = ReadAccountList(reader);
                }
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        private List<Account> ReadAccountList(SqlDataReader reader)
        {
            var list = new List<Account>();
            while (reader.Read())
            {
                var res = new Account();
                res.AccountId = reader.GetInt64(0);
                if (res.AccountId > 0)
                {
                    res.Email = reader.GetString(1);
                    res.FirstName = reader.GetString(2);
                    res.LastName = reader.GetString(3);
                    list.Add(res);
                }
            }
            return list;
        }

        public List<DeviceToken> EnumDeviceTokenOfAccount(long accountId)
        {
            {
                dBContext.OpenConnection();
                var res = new List<DeviceToken>();
                try
                {
                    var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.EnumDeviceTokensOfAccount");
                    dBContext.AddLongParameter(sqlcommand, "AccountId", accountId);
                    using (var reader = sqlcommand.ExecuteReader())
                    {
                        res = ReadDeviceTokenList(reader);
                    }
                    dBContext.CommitTransaction();
                }
                catch (Exception e)
                {
                    dBContext.RollbackTransaction();
                    throw e;
                }
                finally
                {
                    dBContext.CloseConnection();
                }
                return res;
            }
        }

        private List<DeviceToken> ReadDeviceTokenList(SqlDataReader reader)
        {
            var list = new List<DeviceToken>();
            while (reader.Read())
            {
                var res = new DeviceToken();
                res.DeviceTokenId = reader.GetInt64(0);
                if (res.DeviceTokenId > 0)
                {
                    res.AccountId = reader.GetInt64(1);
                    res.Token = reader.GetString(2);
                    res.OSDescription = reader.GetString(3);
                    list.Add(res);
                }
            }
            return list;
        }

        public void DeleteDeviceToken(string deviceToken)
        {
            try
            {
                dBContext.OpenConnection();
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.DeleteDeviceToken");
                dBContext.AddStringParameter(sqlcommand, "DeviceToken", deviceToken);
                sqlcommand.ExecuteNonQuery();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
        }

        public void DeleteRelationship(long accountId, long deviceId)
        {
            try
            {
                dBContext.OpenConnection();
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.DeleteAccountDevice");
                dBContext.AddLongParameter(sqlcommand, "AccountId", accountId);
                dBContext.AddLongParameter(sqlcommand, "DeviceId", deviceId);

                sqlcommand.ExecuteNonQuery();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
        }
    }
}
