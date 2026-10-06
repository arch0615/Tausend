using System.Net;

namespace TausendRelay.Models
{
    public class CentralDevice
    {
        public required int Id { get; set; }
        public required string Identifier { get; set; }
        public required IPAddress Ip { get; set; }
        public required int Port { get; set; }
        public int LastMessageId { get; set; }
        public ushort PublicKey { get; set; }
        // The backend's own numeric DeviceId (distinct from this relay-local Id) -- 0 if the
        // panel isn't registered to any account yet. Needed to call UpdateDeviceLastConnection.
        public long DeviceId { get; set; }
        // Throttles how often a heartbeat actually reaches the backend -- see
        // UdpRelayService.ProcessIdMessageAsync's comment on why every ~10s registration
        // shouldn't become an HTTP call.
        public DateTime? LastBackendSyncUtc { get; set; }
    }
}
