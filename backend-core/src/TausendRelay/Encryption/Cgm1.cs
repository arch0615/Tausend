namespace TausendRelay.Encryption
{
    /// <summary>
    /// Port of the original CGM1 XOR cipher (backend/Tausend.UDPListener/Business/CGM1.cs) --
    /// weak by design (a 16-bit XOR stream cipher, not real encryption), but this is what the
    /// panel firmware speaks, so it's preserved exactly. See RELAY_DECISION.md: fixing this
    /// properly would be a firmware-level change, out of scope here.
    ///
    /// One deliberate behavior change from the original: decript/decriptStr used
    /// Encoding.Default, which means "the OS's current ANSI code page" on .NET Framework but
    /// always means UTF-8 on .NET Core/.NET 8+ -- a silent, platform-dependent meaning change
    /// that would corrupt any non-ASCII byte on this port. The command vocabulary this protocol
    /// actually carries is ASCII-only (see Enums/Commands.cs), and the encrypt side already used
    /// ASCIIEncoding.ASCII explicitly, so both directions now use Encoding.ASCII explicitly --
    /// consistent, unambiguous, and matched to what the protocol actually sends.
    /// </summary>
    public class Cgm1
    {
        private readonly Random _rand = new();
        private ushort _psr;
        private const ushort Cgm1PsrPoly = 0xb400;
        private const uint Cgm1SeedCPrime = 41243;

        private void NextPsr()
        {
            var lsb = _psr & 1;
            _psr >>= 1;
            if (lsb != 0)
                _psr ^= Cgm1PsrPoly;
        }

        private void EncryptDecrypt(byte[] src, int start, int len, ushort seed)
        {
            _psr = seed;
            for (var i = 0; i < len; i++)
            {
                var x = (byte)(_psr & 0xff);
                x ^= src[i + start];
                src[i + start] = x;
                NextPsr();
            }
        }

        private static ushort SeedGenB(ushort kS, ushort kV)
        {
            if (kV < 0x1000)
                kV ^= 0xffff;

            uint x = (uint)kS * kV;
            x += (uint)kS + kV + (uint)(kV >> 8);
            x &= 0xffff;

            return (ushort)x;
        }

        private static ushort SeedGenC(ushort kS, ushort kV)
        {
            uint x = (uint)kS + kV + (uint)(kV >> 8);
            x *= Cgm1SeedCPrime;
            x &= 0xffff;

            return (ushort)x;
        }

        private static ushort SeedGen(ushort kS, ushort kV) => (ushort)(SeedGenB(kS, kV) ^ SeedGenC(kS, kV));

        private ushort KeyVarGen()
        {
            var b = new byte[2];
            _rand.NextBytes(b);
            ushort x = b[1];
            x <<= 8;
            x |= b[0];
            return x;
        }

        private static byte Checksum(byte[] src, int len)
        {
            byte sum = 0;
            for (var i = 0; i < len; i++)
                sum += src[i];
            return sum;
        }

        public string Encrypt(byte[] src, ushort kS)
        {
            var len = src.Length;
            var buffer = new byte[len + 3];

            var kV = KeyVarGen();
            var seed = SeedGen(kS, kV);

            buffer[0] = (byte)(kV & 0xff);
            buffer[1] = (byte)((kV >> 8) & 0xff);

            for (var i = 0; i < len; i++)
                buffer[i + 2] = src[i];

            buffer[len + 2] = Checksum(buffer, len + 2);

            EncryptDecrypt(buffer, 2, len + 1, seed);

            return Convert.ToBase64String(buffer, 0, len + 3);
        }

        public string Encrypt(string src, ushort key) => Encrypt(System.Text.Encoding.ASCII.GetBytes(src), key);

        public byte[] Decrypt(string srcStr, ushort kS)
        {
            var buffer = Convert.FromBase64String(srcStr);
            var len = buffer.Length;
            if (len < 4)
                return [];

            ushort kV = buffer[1];
            kV <<= 8;
            kV |= buffer[0];

            var seed = SeedGen(kS, kV);

            EncryptDecrypt(buffer, 2, len - 2, seed);

            if (buffer[len - 1] != Checksum(buffer, len - 1))
                return [];

            var dest = new byte[len - 3];
            Array.Copy(buffer, 2, dest, 0, len - 3);
            return dest;
        }

        public string DecryptToString(string srcStr, ushort kS) =>
            System.Text.Encoding.ASCII.GetString(Decrypt(srcStr, kS));
    }
}
