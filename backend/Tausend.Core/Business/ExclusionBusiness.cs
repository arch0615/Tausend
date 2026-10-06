using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Business
{
    public class ExclusionBusiness
    {
        private ExclusionDao _dao;
        public ExclusionBusiness()
        {
            _dao = new ExclusionDao();
        }

        public List<Exclusion> EnumExclusions(long deviceId)
        {
            var Exclusions = _dao.EnumExclusions(deviceId);
            if (Exclusions.Count < 32)
            {
                for (int i = 1; i <= 32; i++)
                {
                    if (!Exclusions.Exists(x => x.ExclusionNumber == i))
                    {
                        Exclusions.Add(new Exclusion()
                        {
                            DeviceId = deviceId,
                            Excluded = false,
                            Open = false,
                            Name = i.ToString(),
                            ExclusionNumber = i,
                            ExclusionId = i
                        });
                    }
                }
            }
            return Exclusions;
        }

        public void CreateExclusions(List<Exclusion> Exclusions)
        {
            _dao.CreateExclusions(Exclusions);
        }
    }
}