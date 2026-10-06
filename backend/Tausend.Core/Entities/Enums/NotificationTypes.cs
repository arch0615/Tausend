using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Web;

namespace Tausend.Core.Enums
{
    public enum NotificationTypes
    {
        Unknown,
        Test,
        Medical = 100,
        PersonalMedical = 101,
        Fire = 110,
        Panic = 120,
        Assault = 121,
        SilentPanic = 122,
        KeypadAssault = 123,
        Stole = 130,
        Sabotage = 137,
        ZoneCross = 139,
        Gas = 151,
        SilentAlarm = 146,
        InvalidFail = 300,
        VACLineFail = 301,
        BatteryFail = 302,
        Program = 306,
        Siren1Fail = 321,
        Siren2Fail = 322,
        BusFail = 330,
        Auxiliar12VFail = 337,
        PhoneLineFail = 351,
        CommunicatorFail = 354,
        ServerComunicationFail = 355,
        UserArmDisarm = 401,
        AutoArm = 403,
        AutoArmCanceled = 405,
        BellDisarm = 406,
        FastArm = 408,
        KeyArmDisarm = 409,
        UserAccessControl = 422,
        PointAccessControl = 426,
        AutoArmFail = 455,
        PartArm = 456,
        MemoryDisarmed = 458,
        RecentArmedAlarm = 459,
        ZoneBypass = 570,
        ManualTest = 601,
        PeriodicTest = 602,
        ClockFail = 625,
        UserLink3Fail = 630,
        UserLinked = 631,
        UnknownEvent = 651
    }
}