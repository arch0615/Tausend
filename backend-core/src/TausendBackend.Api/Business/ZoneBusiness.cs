using TausendBackend.Api.Dao;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Business
{
    public class ZoneBusiness
    {
        private readonly ZoneDao _dao;

        public ZoneBusiness(ZoneDao dao)
        {
            _dao = dao;
        }

        /// <summary>
        /// Every panel has 32 zone slots -- any slot without a saved DB row is synthesized here
        /// with its default (unnamed, non-excluded) state so the caller always sees exactly 32.
        /// </summary>
        public List<Zone> EnumZones(long deviceId)
        {
            var zones = _dao.EnumZones(deviceId);
            if (zones.Count < 32)
            {
                for (int i = 1; i <= 32; i++)
                {
                    if (!zones.Exists(x => x.ZoneNumber == i))
                    {
                        zones.Add(new Zone
                        {
                            DeviceId = deviceId,
                            Excluded = false,
                            Open = false,
                            Name = i.ToString(),
                            ZoneNumber = i,
                            ZoneId = i
                        });
                    }
                }
            }
            // Sorted before returning, not just inside the DAO's ORDER BY. The synthesized
            // default slots above are APPENDED to the saved rows, so the list came back as
            // "every named slot in order, then every unnamed slot in order" rather than 1..32.
            // That is the client's issue #3: the zone list read 1, 2, 3, 25, 27, ... and only
            // then 4, 5, 6, and renaming zone 9 appeared to "move" it, because saving a name
            // promoted it out of the appended block into the saved block.
            zones.Sort((a, b) => a.ZoneNumber.CompareTo(b.ZoneNumber));
            return zones;
        }

        public void CreateZones(List<Zone> zones)
        {
            _dao.CreateZone(zones);
        }
    }
}
