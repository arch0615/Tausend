using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tausend.Core.Enums
{
    public enum PhoneOS
    {
        Unknown,
        Android,
        iOS,
        Windows
    }

    public static class PhoneOSHelper
    {
        public static PhoneOS ToPhoneType(this string obj)
        {
            if (obj.ToUpper().Equals("ANDROID"))
                return PhoneOS.Android;
            else if (obj.ToUpper().Equals("IOS"))
                return PhoneOS.iOS;
            else if (obj.ToUpper().Equals("WINDOWS"))
                return PhoneOS.Windows;
            return PhoneOS.Unknown;
        }
    }
}