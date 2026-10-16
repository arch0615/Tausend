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
                if (device is null)
                    return 0;
                var now = DateTime.UtcNow;
                if (device.LastBackendSyncUtc is not null && now - device.LastBackendSyncUtc < minInterval)
                    return 0;
                device.LastBackendSyncUtc = now;
                if (device.DeviceId > 0)
                    return device.DeviceId;
                // DeviceId 0 means the lookup in AddCentralDeviceAsync found nothing, which is
                // what happens when a panel registers before it has an enabled row: powered on
                // but not yet linked to anyone. It stayed 0 for the life of the process, so once
                // somebody did link it the relay never wrote LastConnection again and the panel
                // list showed "Sin conexion" indefinitely even though commands worked (client,
                // 16 Oct). -1 asks the caller to resolve it by identifier once more. The slot is
                // stamped above either way, so a panel that is genuinely unlinked costs one
                // lookup per interval rather than one per heartbeat.
                return -1;
            }
        }

        /// <summary>Stores the backend DeviceId a late lookup resolved, so the next heartbeat can
        /// sync normally. Also stamps the sync slot, since the caller is about to use it.</summary>
        public void SetBackendDeviceId(int id, long deviceId)
        {
            lock (_lock)
            {
                var device = _devices.Find(d => d.Id == id);
                if (device is null || deviceId <= 0)
                    return;
                device.DeviceId = deviceId;
                device.LastBackendSyncUtc = DateTime.UtcNow;
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
