using System.Configuration;
using System.ServiceModel.Web;

namespace Tausend.Core.Security
{
    /// <summary>
    /// Deliberate duplicate of Tausend.Core's Security\SystemAuth.cs: this project has no
    /// assembly/project reference to Tausend.Core (see the other Tausend.Core.* types already
    /// duplicated under Entities\ in this project, e.g. CommandResponse, DeviceResponse), so the
    /// shared-secret check for backend<->relay system calls is copied rather than shared. Both
    /// copies must stay identical -- see backend/RELAY_DECISION.md.
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
