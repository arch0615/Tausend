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
            return zones;
        }

        public void CreateZones(List<Zone> zones)
        {
            _dao.CreateZone(zones);
        }
    }
}
