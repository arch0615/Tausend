namespace TausendBackend.Api.Security
{
    /// <summary>
    /// Shared-secret check for system-to-system calls between this backend and the relay
    /// (Tausend.UDPListener) -- as opposed to AccountBusiness.ValidateAccessToken, which is for
    /// end-user calls. Behaviorally mirrors Tausend.Core.Security.SystemAuth (the WCF backend's
    /// copy); see ../backend/RELAY_DECISION.md and ../backend/DAY5_SUMMARY.md for why this
    /// exists. Unlike the WCF version, there's no static ambient "current request" here --
    /// callers (controllers) read the incoming header and the configured key themselves (via
    /// HttpContext/IConfiguration, both already available through DI) and pass both in.
    /// </summary>
    public static class SystemAuth
    {
        public const string HeaderName = "X-System-Key";

        /// <summary>Convenience for controllers: reads the incoming header and the configured
        /// key and compares them in one call.</summary>
        public static bool IsValidRequest(HttpRequest request, IConfiguration configuration)
        {
            var incoming = request.Headers[HeaderName].FirstOrDefault();
            var expected = configuration["RelaySystemKey"];
            return KeysMatch(expected, incoming);
        }

        public static bool KeysMatch(string? expectedKey, string? incomingKey)
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
