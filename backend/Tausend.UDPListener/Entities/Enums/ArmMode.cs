using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.RelayServer.Enums
{
    public enum ArmMode
    {
        STAY,
        AWAY,
        NIGHT
    }

    public static class ArmModeHelper
    {
        public static string ToArmModeChar(this ArmMode mode)
        {
            switch (mode)
            {
                case ArmMode.STAY:
                    return "S";
                case ArmMode.AWAY:
                    return "A";
                case ArmMode.NIGHT:
                    return "N";
                default:
                    throw new Exception("Invalid ArmMode");
            }
        }
    }
}
