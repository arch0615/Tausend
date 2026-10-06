using System.Net;
using TausendRelay.Business;
using TausendRelay.Models;

namespace TausendRelay.Cache
{
    /// <summary>
    /// In-memory panel registry, port of backend/Tausend.UDPListener/Cache/CacheManager.cs.
    ///
    /// One correctness fix from the original: CacheManager's device list and id counter were
    /// static, unsynchronized mutable state read/written from two concurrent contexts -- the
    /// single UDP receive loop and, separately, whichever thread-pool thread is handling a
    /// concurrent PrivateService HTTP request. The original WCF host serialized PrivateService
    /// calls to one at a time (ConcurrencyMode.Single) but that only protects PrivateService
    /// against itself; it never protected the receive loop against PrivateService. Kestrel gives
    /// no such serialization at all, so this is now an explicit lock around every read/write of
    /// the shared list -- the backend lookup that can precede an add runs *before* the lock is
    /// taken, so a slow HTTP round trip to the backend never blocks other panels' UDP traffic.
    /// </summary>
    public class DeviceCache
    {
        private const int MaxDevices = 10000;

        private readonly List<CentralDevice> _devices = [];
        private readonly object _lock = new();
        private int _nextId = 1;
        private readonly BackendClient _backendClient;

        public DeviceCache(BackendClient backendClient)
        {
            _backendClient = backendClient;
        }

        public async Task<CentralDevice?> AddCentralDeviceAsync(string identifier, IPAddress ip, int port)
        {
            var dbDevice = await _backendClient.GetDeviceAsync(identifier);

            lock (_lock)
            {
                if (_nextId >= MaxDevices)
                    return null;
                var device = new CentralDevice
                {
                    Id = _nextId,
                    Identifier = identifier,
                    Ip = ip,
                    Port = port,
                    PublicKey = dbDevice?.PublicKey ?? 0,
                    DeviceId = dbDevice?.DeviceId ?? 0,
                };
                _devices.Add(device);
                _nextId++;
                return device;
            }
        }

        public CentralDevice? GetById(int id)
        {
            lock (_lock)
                return _devices.Find(d => d.Id == id);
        }

        public CentralDevice? GetByIdentifier(string identifier)
        {
            lock (_lock)
                return _devices.Find(d => d.Identifier == identifier);
        }

        public void Refresh(int id, IPAddress ip, int port)
        {
            lock (_lock)
            {
                var device = _devices.Find(d => d.Id == id);
                if (device is null)
                    return;
                device.Ip = ip;
                device.Port = port;
            }
        }

        // Claims this device's next backend-sync slot if enough time has passed, returning its
        // backend DeviceId (0 if it isn't linked to any account yet -- nothing to update).
        // Claiming immediately (not after the HTTP call completes) means two heartbeats arriving
        // close together only ever produce one outbound call, not one that also races a second.
        public long TryClaimBackendSync(int id, TimeSpan minInterval)
        {
            lock (_lock)
            {
                var device = _devices.Find(d => d.Id == id);
                if (device is null || device.DeviceId <= 0)
                    return 0;
                var now = DateTime.UtcNow;
                if (device.LastBackendSyncUtc is not null && now - device.LastBackendSyncUtc < minInterval)
                    return 0;
                device.LastBackendSyncUtc = now;
                return device.DeviceId;
            }
        }

        public bool IsNewMessage(int id, int messageId)
        {
            lock (_lock)
            {
                var device = _devices.Find(d => d.Id == id);
                if (device is null)
                    return false;
                if (messageId != 0 && device.LastMessageId >= messageId)
                    return false;
                device.LastMessageId = messageId;
                return true;
            }
        }
    }
}
