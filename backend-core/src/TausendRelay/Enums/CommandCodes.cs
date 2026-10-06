namespace TausendRelay.Enums
{
    /// <summary>Panel command vocabulary and framing punctuation -- port of
    /// backend/Tausend.UDPListener/Entities/Enums/COMMANDS.cs.</summary>
    public static class CommandCodes
    {
        public const string Version = "VER";
        public const string ProgramSection = "PRG";
        public const string ProgramControl = "PGM";
        public const string Status = "STS";
        public const string Clock = "RTC";
        public const string Bypass = "BYP";
        public const string Arm = "ARM";
        public const string Disarm = "DAR";
        public const string FirmwareUpdate = "FWU";
        public const string SpecialFunction = "FUN";

        public const string IpCommandStartChar = "<";
        public const string IpCommandEndChar = ">";
        public const string SmsCommandStartChar = "*";
        public const string SmsCommandEndChar = "*";
        public const string ParamsChar = ":";
        public const string SeparatorChar = ",";
        public const string Default = "DEF";
    }
}
