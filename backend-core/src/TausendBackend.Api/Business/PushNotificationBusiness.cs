using TausendBackend.Api.Models.Notifications;

namespace TausendBackend.Api.Business
{
    public abstract class PushNotificationBusiness
    {
        protected abstract string SendNotification(NotificationBase notification);
        protected abstract void GetNotificationSettings();
        private readonly NotificationBase _notification;

        public PushNotificationBusiness(NotificationBase notification)
        {
            _notification = notification;
        }

        public string SendPushNotification()
        {
            GetNotificationSettings();
            return SendNotification(_notification);
        }
    }
}
