using TausendBackend.Api.Dao;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Business
{
    public class ProgramControlBusiness
    {
        private readonly ProgramControlDao _dao;
        private readonly CommandBusiness _commandBusiness;

        public ProgramControlBusiness(ProgramControlDao dao, CommandBusiness commandBusiness)
        {
            _dao = dao;
            _commandBusiness = commandBusiness;
        }

        /// <summary>
        /// Every panel has 8 PGM output slots. Unlike Zones/Exclusions this doesn't just fill in
        /// defaults for missing DB rows -- it also live-queries each of the 8 outputs' current
        /// activation state from the panel via CommandBusiness.SendGetPGMCommand (PGM&lt;n&gt;) and
        /// overlays that onto whatever row (saved or synthesized) ends up in the result, matching
        /// the original Tausend.Core.Business.ProgramControlBusiness. CommandBusiness's relay call
        /// is now async, so this is async too (it used to block synchronously on the WCF client).
        /// </summary>
        public async Task<List<ProgramControl>> EnumProgramControls(long deviceId, string accessToken)
        {
            var programControls = _dao.EnumProgramControls(deviceId);
            if (programControls.Count < 8)
            {
                for (int i = 1; i <= 8; i++)
                {
                    var activated = await _commandBusiness.SendGetPGMCommand(accessToken, deviceId, i);
                    var existing = programControls.Find(x => x.ProgramControlNumber == i);
                    if (existing == null)
                    {
                        programControls.Add(new ProgramControl
                        {
                            DeviceId = deviceId,
                            Activated = activated,
                            Name = i.ToString(),
                            ProgramControlNumber = i,
                            ProgramControlId = i
                        });
                    }
                    else
                    {
                        existing.Activated = activated;
                    }
                }
            }
            // Sorted before returning, not just inside the DAO's ORDER BY. The synthesized
            // default slots above are APPENDED to the saved rows, so the list came back as
            // "every named slot in order, then every unnamed slot in order" rather than 1..32.
            // That is the client's issue #3, same shape: the output list read 1, 2, 3, 25, 27, ... and only
            // then 4, 5, 6, and renaming zone 9 appeared to "move" it, because saving a name
            // promoted it out of the appended block into the saved block.
            programControls.Sort((a, b) => a.ProgramControlNumber.CompareTo(b.ProgramControlNumber));
            return programControls;
        }

        public void CreateProgramControls(List<ProgramControl> programControls)
        {
            _dao.CreateProgramControls(programControls);
        }
    }
}
