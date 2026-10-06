using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Enums
{
    public enum ResponseStates
    {
        OK = 0,
        SERVER_ERROR = 1,
        BUSSINESS_ERROR = 2,
        UNAUTHORIZED = 3,
        WRONG_DATA = 4,
        NOT_FOUND = 5,
        CENTRAL_UNRESPONSIVE = 6
    }
}