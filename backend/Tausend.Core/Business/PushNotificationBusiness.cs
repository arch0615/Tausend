using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Entities.Notifications;
using Tausend.Core.Models;

namespace Tausend.Core.Business
{
    public abstract class PushNotificationBusiness
    {
        protected abstract string SendNotification(NotificationBase notification);
        protected abstract void GetNotificationSettings();
        private NotificationBase Notification;
        public PushNotificationBusiness(NotificationBase notification)
        {
            Notification = notification;
        }

        public string SendPushNotification()
        {
            GetNotificationSettings();
            return SendNotification(Notification);
        }
    }
}