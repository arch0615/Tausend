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
            // Sorted before returning, not just inside the DAO's ORDER BY. The synthesized
            // default slots above are APPENDED to the saved rows, so the list came back as
            // "every named slot in order, then every unnamed slot in order" rather than 1..32.
            // That is the client's issue #3, same shape: the exclusion list read 1, 2, 3, 25, 27, ... and only
            // then 4, 5, 6, and renaming zone 9 appeared to "move" it, because saving a name
            // promoted it out of the appended block into the saved block.
            exclusions.Sort((a, b) => a.ExclusionNumber.CompareTo(b.ExclusionNumber));
            return exclusions;
        }

        public void CreateExclusions(List<Exclusion> exclusions)
        {
            _dao.CreateExclusions(exclusions);
        }
    }
}
