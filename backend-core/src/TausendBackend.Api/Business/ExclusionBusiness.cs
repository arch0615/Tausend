using TausendBackend.Api.Dao;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Business
{
    public class ExclusionBusiness
    {
        private readonly ExclusionDao _dao;

        public ExclusionBusiness(ExclusionDao dao)
        {
            _dao = dao;
        }

        /// <summary>
        /// Every panel has 32 exclusion slots -- any slot without a saved DB row is synthesized
        /// here with its default (unnamed, non-excluded) state so the caller always sees exactly 32.
        /// </summary>
        public List<Exclusion> EnumExclusions(long deviceId)
        {
            var exclusions = _dao.EnumExclusions(deviceId);
            if (exclusions.Count < 32)
            {
                for (int i = 1; i <= 32; i++)
                {
                    if (!exclusions.Exists(x => x.ExclusionNumber == i))
                    {
                        exclusions.Add(new Exclusion
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
            return exclusions;
        }

        public void CreateExclusions(List<Exclusion> exclusions)
        {
            _dao.CreateExclusions(exclusions);
        }
    }
}
