namespace TausendRelay.Security
{
    /// <summary>
    /// Shared-secret check for system-to-system calls between this relay and the backend. This is
    /// a deliberate duplicate of TausendBackend.Api's own Security/SystemAuth.cs (which is itself
    /// a duplicate of the legacy backend's copy) -- each side reads its own local config and
    /// compares independently, no shared assembly reference. All copies must stay identical; see
    /// ../../../backend/RELAY_DECISION.md.
    /// </summary>
    public static class SystemAuth
    {
        public const string HeaderName = "X-System-Key";

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
