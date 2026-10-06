using Microsoft.Data.SqlClient;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Models;
using TausendBackend.Api.Security;

namespace TausendBackend.Api.Dao
{
    public class AccountDao
    {
        private readonly SqlDbContext _db;

        public AccountDao(SqlDbContext db)
        {
            _db = db;
        }

        public long CreateAccount(Account account)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var passwordHash = PasswordHasher.HashPassword(account.Password ?? "");
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.CreateAccount");
                _db.AddStringParameter(sqlcommand, "FirstName", account.FirstName);
                _db.AddStringParameter(sqlcommand, "LastName", account.LastName);
                _db.AddStringParameter(sqlcommand, "EMail", account.Email);
                _db.AddStringParameter(sqlcommand, "PasswordHash", passwordHash);
                var x = sqlcommand.ExecuteScalar();
                res = (long)x!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public long ValidateAccessToken(string accessToken)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.ValidateAccessToken");
                _db.AddStringParameter(sqlcommand, "AccessToken", accessToken);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public Account? Login(string email, string password)
        {
            _db.OpenConnection();
            Account? res = null;
            try
            {
                var credentials = GetLoginCredentials(email);
                if (credentials != null && credentials.Enabled && VerifyPassword(password, credentials))
                {
                    var sqlcommand = _db.CreateStoredProcedureCommand("dbo.CreateLoginSession");
                    _db.AddLongParameter(sqlcommand, "AccountId", credentials.AccountId);
                    using var reader = sqlcommand.ExecuteReader();
                    res = GetAccountFromReader(reader);
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        /// <summary>Raw credential row for an account, used only to verify a password in C#.</summary>
        private class LoginCredentials
        {
            public long AccountId;
            public string? PasswordHash;
            public byte[]? LegacyPassword;
            public bool Enabled;
        }

        private LoginCredentials? GetLoginCredentials(string email)
        {
            var sqlcommand = _db.CreateStoredProcedureCommand("dbo.GetLoginCredentials");
            _db.AddStringParameter(sqlcommand, "Email", email);
            using var reader = sqlcommand.ExecuteReader();
            return ReadLoginCredentials(reader);
        }

        private LoginCredentials? GetAccountCredentialsByToken(string accessToken)
        {
            var sqlcommand = _db.CreateStoredProcedureCommand("dbo.GetAccountCredentialsByToken");
            _db.AddStringParameter(sqlcommand, "AccessToken", accessToken);
            using var reader = sqlcommand.ExecuteReader();
            return ReadLoginCredentials(reader);
        }

        private LoginCredentials? ReadLoginCredentials(SqlDataReader reader)
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
            var sqlcommand = _db.CreateStoredProcedureCommand("dbo.SetPasswordHash");
            _db.AddLongParameter(sqlcommand, "AccountId", accountId);
            _db.AddStringParameter(sqlcommand, "PasswordHash", passwordHash);
            sqlcommand.ExecuteNonQuery();
        }

        public Account? RefreshAccessToken(string refreshToken)
        {
            _db.OpenConnection();
            string? newAccessToken = null;
            string? newRefreshToken = null;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.RefreshAccessToken");
                _db.AddStringParameter(sqlcommand, "RefreshToken", refreshToken);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var accountId = reader.GetInt64(0);
                        if (accountId > 0)
                        {
                            newAccessToken = reader.IsDBNull(1) ? null : reader.GetString(1);
                            newRefreshToken = reader.IsDBNull(2) ? null : reader.GetString(2);
                        }
                    }
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }

            if (newAccessToken == null) return null;

            // dbo.RefreshAccessToken only rotates the tokens -- it never returned the rest of
            // the account (Devices/SmsDevices/name/email/role), unlike Login. That left every
            // screen thinking the account had zero panels after a cold app relaunch or an
            // in-session refresh, until the next full Login re-fetched everything. Fetch the
            // full account the same way GetAccount(accessToken) does, then attach the freshly
            // rotated refresh token (GetAccount's own query never returns one -- see
            // GetAccountFromReader's comment on CreateLoginSession's third result set).
            var res = GetAccount(newAccessToken);
            if (res != null)
            {
                res.RefreshToken = newRefreshToken ?? "";
            }
            return res;
        }

        public Account? GetAccount(string accessToken)
        {
            _db.OpenConnection();
            Account? res = null;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.GetAccount");
                _db.AddStringParameter(sqlcommand, "AccessToken", accessToken);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    res = GetAccountFromReader(reader);
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public void DeleteAccount(string token)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.DeleteAccount");
                _db.AddStringParameter(sqlCommand, "AccessToken", token);
                sqlCommand.ExecuteNonQuery();
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
        }

        public void Logout(string accessToken, string deviceToken)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.Logout");
                _db.AddStringParameter(sqlCommand, "AccessToken", accessToken);
                _db.AddStringParameter(sqlCommand, "DeviceToken", deviceToken);
                sqlCommand.ExecuteNonQuery();
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
        }

        private Account? GetAccountFromReader(SqlDataReader reader)
        {
            Account? res = null;
            while (reader.Read())
            {
                var accountId = reader.GetInt64(0);
                if (accountId > 0)
                {
                    res = new Account { AccountId = accountId };
                    res.Email = reader.GetString(1);
                    res.FirstName = reader.GetString(2);
                    res.LastName = reader.GetString(3);
                    res.AccessToken = reader.GetString(4);
                    var ad = new AccountDevice
                    {
                        Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Mac = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        Pin = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        DeviceId = reader.IsDBNull(8) ? 0 : reader.GetInt64(8),
                        // Was never read here at all -- every device the app ever displayed an
                        // online/offline status for showed offline regardless of the panel's
                        // actual, correctly-tracked connection state (see GetDevice.sql, which
                        // does read this column correctly for command-sending purposes).
                        IsOnline = !reader.IsDBNull(10) && reader.GetBoolean(10)
                    };
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
                    var device = new AccountSmsDevice
                    {
                        DeviceId = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                        Description = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Identifier = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        DevicePin = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        SimPin = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        DeviceType = reader.IsDBNull(6) ? "" : reader.GetString(6)
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
            _db.OpenConnection();
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
                    var sqlCommand = _db.CreateStoredProcedureCommand("dbo.ChangePassword");
                    _db.AddLongParameter(sqlCommand, "AccountId", credentials.AccountId);
                    _db.AddStringParameter(sqlCommand, "PasswordHash", PasswordHasher.HashPassword(newPassword));
                    sqlCommand.ExecuteNonQuery();
                    res = credentials.AccountId;
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public class PasswordResetTokenInfo
        {
            public long AccountId;
            public string ResetToken = "";
            public string Email = "";
            public string FirstName = "";
        }

        /// <summary>Returns null if the email doesn't match an enabled account (caller should still report success to avoid leaking account existence).</summary>
        public PasswordResetTokenInfo? CreatePasswordResetToken(string email)
        {
            _db.OpenConnection();
            PasswordResetTokenInfo? res = null;
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreatePasswordResetToken");
                _db.AddStringParameter(sqlCommand, "Email", email);
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
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        /// <summary>Returns the account id on success, -1 if the token is missing/expired/already used.</summary>
        public long ResetPasswordWithToken(string resetToken, string newPassword)
        {
            long res = -1;
            _db.OpenConnection();
            try
            {
                var passwordHash = PasswordHasher.HashPassword(newPassword);
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.ResetPasswordWithToken");
                _db.AddStringParameter(sqlCommand, "ResetToken", resetToken);
                _db.AddStringParameter(sqlCommand, "NewPasswordHash", passwordHash);
                res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public long CreateDeviceToken(NewDeviceToken token)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.CreateDeviceToken");
                _db.AddStringParameter(sqlcommand, "AccessToken", token.AccessToken);
                _db.AddStringParameter(sqlcommand, "OS", token.OS);
                _db.AddStringParameter(sqlcommand, "DeviceToken", token.Token);
                var x = sqlcommand.ExecuteScalar();
                res = (long)x!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public List<Account> EnumOwnersOfDevice(string identifier)
        {
            _db.OpenConnection();
            var res = new List<Account>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumOwnersOfDevice");
                _db.AddStringParameter(sqlcommand, "Identifier", identifier);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    res = ReadAccountList(reader);
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        private List<Account> ReadAccountList(SqlDataReader reader)
        {
            var list = new List<Account>();
            while (reader.Read())
            {
                var res = new Account { AccountId = reader.GetInt64(0) };
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
            _db.OpenConnection();
            var res = new List<DeviceToken>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumDeviceTokensOfAccount");
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    res = ReadDeviceTokenList(reader);
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        private List<DeviceToken> ReadDeviceTokenList(SqlDataReader reader)
        {
            var list = new List<DeviceToken>();
            while (reader.Read())
            {
                var res = new DeviceToken { DeviceTokenId = reader.GetInt64(0) };
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
            _db.OpenConnection();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.DeleteDeviceToken");
                _db.AddStringParameter(sqlcommand, "DeviceToken", deviceToken);
                sqlcommand.ExecuteNonQuery();
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
        }

        public void DeleteRelationship(long accountId, long deviceId)
        {
            _db.OpenConnection();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.DeleteAccountDevice");
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                sqlcommand.ExecuteNonQuery();
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
        }
    }
}
