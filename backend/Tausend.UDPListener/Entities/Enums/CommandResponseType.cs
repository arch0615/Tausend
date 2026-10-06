using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.RelayServer.Entities.Enums
{
    public enum CommandResponseType
    {
        None,
        Generic,
        ZoneStatus,
        GeneralStatus,
        Disarm,
        Arm,
        Bypass,
        Version,
        FailStatus,
        Memory,
        Installer,
        ProgramControl,
        Clock,
        Program,
        Battery
    }
}
