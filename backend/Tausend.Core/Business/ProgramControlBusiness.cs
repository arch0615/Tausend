using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Backend.Business;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Business
{
    public class ProgramControlBusiness
    {
        private ProgramControlDao _dao;
        public ProgramControlBusiness()
        {
            _dao = new ProgramControlDao();
        }

        public List<ProgramControl> EnumProgramControls(long deviceId, string accessToken)
        {
            var cmdBz = new CommandBusiness();
            var ProgramControls = _dao.EnumProgramControls(deviceId);
            if (ProgramControls.Count < 8)
            {
                for (int i = 1; i <= 8; i++)
                {
                    var activated = cmdBz.SendGetPGMCommand(accessToken, deviceId, i);
                    if (!ProgramControls.Exists(x => x.ProgramControlNumber == i))
                    {
                        ProgramControls.Add(new ProgramControl()
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
                        var pgm = ProgramControls.Find(x => x.ProgramControlNumber == i);
                        pgm.Activated = activated;
                    }
                }
            }
            return ProgramControls;
        }

        public void CreateProgramControls(List<ProgramControl> ProgramControls)
        {
            _dao.CreateProgramControls(ProgramControls);
        }
    }
}