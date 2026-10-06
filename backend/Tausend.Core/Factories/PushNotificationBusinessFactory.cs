using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Business;
using Tausend.Core.Entities.Notifications;
using Tausend.Core.Models;

namespace Tausend.Core.Factories
{
    public class PushNotificationBusinessFactory
    {
        private static readonly Lazy<PushNotificationBusinessFactory> _lazy = new Lazy<PushNotificationBusinessFactory>(() => new PushNotificationBusinessFactory());
        public static PushNotificationBusinessFactory Instance
        {
            get
            {
                return _lazy.Value;
            }
        }

        private PushNotificationBusinessFactory()
        {
        }

        public PushNotificationBusiness CreateNotificationBusiness(NotificationBase notification)
        {
            return new FirebasePushNotificationBusiness(notification);
            //switch (notification.OS)
            //{
            //    case Enums.PhoneOS.Android:
            //        return new FirebasePushNotificationBusiness(notification);
            //    case Enums.PhoneOS.iOS:
            //        return new FirebasePushNotificationBusiness(notification);
            //    case Enums.PhoneOS.Windows:
            //        break;
            //    default:
            //        break;
            //}
            //throw new NotImplementedException();
        }
    }
}