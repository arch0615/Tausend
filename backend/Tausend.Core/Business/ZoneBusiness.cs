using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Business
{
    public class ZoneBusiness
    {
        private ZoneDao _dao;
        public ZoneBusiness()
        {
            _dao = new ZoneDao();
        }

        public List<Zone> EnumZones(long deviceId)
        {
            var zones = _dao.EnumZones(deviceId);
            if (zones.Count < 32)
            {
                for (int i = 1; i <= 32; i++)
                {
                    if (!zones.Exists(x => x.ZoneNumber == i))
                    {
                        zones.Add(new Zone()
                        {
                            DeviceId = deviceId,
                            Excluded = false,
                            Open = false,
                            Name = i.ToString(),
                            ZoneNumber = i,
                            ZoneId = i
                        }) ;
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