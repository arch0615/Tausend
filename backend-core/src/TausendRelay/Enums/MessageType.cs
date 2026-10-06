namespace TausendRelay.Enums
{
    /// <summary>Wire-level message type byte -- port of
    /// backend/Tausend.UDPListener/Entities/Enums/MessageType.cs.</summary>
    public enum MessageType
    {
        Unknown = 0x00,
        // Panel -> relay
        Id = 0x01,
        Idq = 0x02,
        Data = 0x03,
        Con = 0x04,
        Event = 0x05,
        // Relay -> panel
        IdOk = 0x81,
        IdFull = 0x82,
        IdqOk = 0x83,
        IdqNe = 0x84,
        ConOk = 0x85,
        ConRj = 0x86,
        EventOk = 0x87,
    }
}
