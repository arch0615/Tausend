using System.Security.Cryptography;
using System.Text;

namespace TausendBackend.Api.Security
{
    /// <summary>
    /// Bcrypt hashing for new/updated passwords, plus a one-time verifier for the
    /// legacy unsalted SHA-256 hashes still sitting in Accounts.Password for accounts
    /// that haven't logged in since the migration (see AccountDao.VerifyPassword).
    /// </summary>
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyBcrypt(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// Reproduces the old dbo.Encrypt T-SQL function: HASHBYTES('SHA2_256', @Password)
        /// on an NVARCHAR parameter hashes the UTF-16LE bytes of the string, not UTF-8.
        /// </summary>
        public static bool VerifyLegacySha256(string password, byte[]? legacyHash)
        {
            if (legacyHash == null)
                return false;
            var computed = SHA256.HashData(Encoding.Unicode.GetBytes(password ?? ""));
            return computed.SequenceEqual(legacyHash);
        }
    }
}
