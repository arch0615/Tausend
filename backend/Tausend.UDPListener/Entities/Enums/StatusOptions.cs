using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.RelayServer.Enums
{
    public enum StatusOptions
    {
        ZONE,
        EXCLUSION,
        MEMORY,
        FAIL,
        GENERAL_STATUS,
        COMMUNICATOR,
        BATTERY
    }

    public static class StatusOptionHelper
    {
        public static string ToOptionChar(this StatusOptions status)
        {
            switch (status)
            {
                case StatusOptions.ZONE:
                    return "Z";
                case StatusOptions.EXCLUSION:
                    return "X";
                case StatusOptions.MEMORY:
                    return "M";
                case StatusOptions.FAIL:
                    return "F";
                case StatusOptions.GENERAL_STATUS:
                    return "A";
                case StatusOptions.COMMUNICATOR:
                    return "C";
                case StatusOptions.BATTERY:
                    return "B";
                default:
                    throw new Exception("Invalid option for status");
            }
        }
    }
}
