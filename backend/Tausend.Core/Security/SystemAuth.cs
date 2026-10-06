using System.Configuration;
using System.ServiceModel.Web;

namespace Tausend.Core.Security
{
    /// <summary>
    /// Shared-secret check for system-to-system calls between Tausend.Core and the relay
    /// (Tausend.UDPListener) -- as opposed to AccountBusiness.ValidateAccessToken, which is
    /// for end-user calls. Both processes read the same RelaySystemKey value from their own
    /// local config (Tausend.Core's Secrets.config, Tausend.UDPListener's Secrets.config) and
    /// must be provisioned with the identical value for either leg of the relay integration
    /// to authenticate.
    /// </summary>
    public static class SystemAuth
    {
        public const string HeaderName = "X-System-Key";

        public static string ConfiguredKey => ConfigurationManager.AppSettings["RelaySystemKey"];

        public static bool IsValidRequest()
        {
            var incoming = WebOperationContext.Current?.IncomingRequest?.Headers[HeaderName];
            return IsValidKey(incoming);
        }

        /// <summary>
        /// The comparison logic on its own, independent of WebOperationContext (which is only
        /// populated during a live WCF request) -- lets this be exercised directly in a unit
        /// test by passing the header value in, rather than needing a running service host.
        /// </summary>
        public static bool IsValidKey(string incomingKey)
        {
            return KeysMatch(ConfiguredKey, incomingKey);
        }

        /// <summary>
        /// Fully pure comparison -- independent of WebOperationContext AND ConfigurationManager
        /// (which, past the appSettings section, is process-wide and not safely mutable from a
        /// test). Exists so the comparison rules themselves (empty-key handling, fixed-time
        /// compare) can be unit tested without touching either.
        /// </summary>
        public static bool KeysMatch(string expectedKey, string incomingKey)
        {
            if (string.IsNullOrEmpty(expectedKey))
                return false;
            return !string.IsNullOrEmpty(incomingKey) && FixedTimeEquals(expectedKey, incomingKey);
        }

        // Avoids leaking key-length/prefix information through response-time differences.
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
