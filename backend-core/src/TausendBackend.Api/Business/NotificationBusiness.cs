using System.Globalization;
using TausendBackend.Api.Dao;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Models;
using TausendBackend.Api.Models.Notifications;
using TausendBackend.Api.Requests;

namespace TausendBackend.Api.Business
{
    public class NotificationBusiness
    {
        private readonly AccountBusiness _accountBusiness;
        private readonly DeviceBusiness _deviceBusiness;
        private readonly UserBusiness _userBusiness;
        private readonly NotificationBuilder _notificationBuilder;
        private readonly PushNotificationBusinessFactory _pushFactory;
        private readonly EventDao _eventDao;
        private readonly NotificationMuteDao _muteDao;

        public NotificationBusiness(
            AccountBusiness accountBusiness,
            DeviceBusiness deviceBusiness,
            UserBusiness userBusiness,
            NotificationBuilder notificationBuilder,
            PushNotificationBusinessFactory pushFactory,
            EventDao eventDao,
            NotificationMuteDao muteDao)
        {
            _accountBusiness = accountBusiness;
            _deviceBusiness = deviceBusiness;
            _userBusiness = userBusiness;
            _notificationBuilder = notificationBuilder;
            _pushFactory = pushFactory;
            _eventDao = eventDao;
            _muteDao = muteDao;
        }

        /// <summary>Client, 27 Oct: "Apagar notificaciones" on Gestionar central -- mutes this one
        /// phone's push for this one panel, without affecting any other account/phone sharing
        /// it.</summary>
        public void SetNotificationMute(string deviceToken, string identifier, bool muted) =>
            _muteDao.SetMuted(deviceToken, identifier, muted);

        public bool IsNotificationMuted(string deviceToken, string identifier) =>
            _muteDao.IsMuted(deviceToken, identifier);

        public string SendEmergencyNotification(long deviceId)
        {
            try
            {
                // System lookup (notifying every owner of the device), not scoped to one account.
                var device = _deviceBusiness.GetDevice(deviceId, 0);
                var identifier = device.Device?.Identifier ?? "";
                var owners = _accountBusiness.EnumOwnersOfDevice(identifier);
                foreach (var account in owners)
                {
                    var deviceTokens = _accountBusiness.EnumDeviceTokenOfAccount(account.AccountId);
                    foreach (var item in deviceTokens)
                    {
                        // Client, 27 Oct: "Apagar notificaciones" -- skip only the phones that
                        // muted this specific panel, not the account's other phones or other
                        // accounts sharing it.
                        if (_muteDao.IsMuted(item.Token, identifier)) continue;
                        _notificationBuilder.NotificationType = NotificationTypes.Medical;
                        _notificationBuilder.DeviceToken = item.Token;
                        _notificationBuilder.OS = item.OS;
                        var notif = _notificationBuilder.Build();
                        notif.DeviceDescription = device.Device?.Description;
                        _pushFactory.CreateNotificationBusiness(notif).SendPushNotification();
                    }
                }
                return "OK";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string ProcessNotificationRequest(NotificationRequest request)
        {
            try
            {
                var owners = _accountBusiness.EnumOwnersOfDevice(request.AlarmIdentifier);
                // Built once, independent of whether any push token exists to send it to -- until
                // push notifications are fully wired up (see src/notifications/README.md) no
                // account has a registered device token, so the loop below never runs and, before
                // this, the event's Text silently stayed empty forever. Uses the first owner (or
                // account 0, matching SendEmergencyNotification's "system lookup" convention above,
                // if the device is somehow unowned) purely to resolve the device description/user
                // label -- the resulting Body text is identical for every owner regardless of which
                // one's account id happens to be used for that lookup.
                var body = BuildEventBody(request, owners.FirstOrDefault()?.AccountId ?? 0);
                foreach (var account in owners)
                {
                    var deviceTokens = _accountBusiness.EnumDeviceTokenOfAccount(account.AccountId);
                    foreach (var item in deviceTokens)
                    {
                        // Client, 27 Oct: "Apagar notificaciones" -- skip only the phones that
                        // muted this specific panel, not the account's other phones or other
                        // accounts sharing it. The event itself is still logged below regardless,
                        // this only withholds the push.
                        if (_muteDao.IsMuted(item.Token, request.AlarmIdentifier)) continue;
                        _notificationBuilder.NotificationType = (NotificationTypes)request.NotificationType;
                        _notificationBuilder.AlarmParameter = request.AlarmParameter;
                        _notificationBuilder.EventType = request.EventType;
                        _notificationBuilder.DeviceToken = item.Token;
                        _notificationBuilder.OS = item.OS;
                        _notificationBuilder.Icon = "notification_icon";
                        _notificationBuilder.Sound = "default";
                        _notificationBuilder.AlarmIdentifier = request.AlarmIdentifier;
                        _notificationBuilder.AccountId = account.AccountId;
                        _notificationBuilder.Date = request.EventDateTime;
                        _notificationBuilder.Email = request.Email;
                        var notif = _notificationBuilder.Build();
                        SetUserName(notif, request.AlarmIdentifier, account.AccountId);
                        _pushFactory.CreateNotificationBusiness(notif).SendPushNotification();
                    }
                }
                CreateEvent(request, body);
                return "OK";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        /// <summary>Builds just the notification body text, using a placeholder token/OS that
        /// satisfies NotificationBuilder's validation but is never handed to a real push send --
        /// Body's content (NotificationBase.GetBody()) never reads DeviceToken or OS, only
        /// Date/EventType/AlarmParameter/UserName/DeviceDescription, so this is safe.</summary>
        private string BuildEventBody(NotificationRequest request, long accountId)
        {
            _notificationBuilder.NotificationType = (NotificationTypes)request.NotificationType;
            _notificationBuilder.AlarmParameter = request.AlarmParameter;
            _notificationBuilder.EventType = request.EventType;
            _notificationBuilder.DeviceToken = "event-log-only";
            _notificationBuilder.OS = PhoneOS.Android;
            _notificationBuilder.AlarmIdentifier = request.AlarmIdentifier;
            _notificationBuilder.AccountId = accountId;
            _notificationBuilder.Date = request.EventDateTime;
            var notif = _notificationBuilder.Build();
            SetUserName(notif, request.AlarmIdentifier, accountId);
            return notif.Body;
        }

        public void SetUserName(NotificationBase notif, string identifier, long accountId)
        {
            try
            {
                var device = _deviceBusiness.GetDevice(identifier, accountId);
                notif.DeviceDescription = device.Device?.Description;
                if (!notif.UseUserParam) return;
                var users = _userBusiness.EnumUsers(device.Device?.DeviceId ?? 0);
                var userTag = users.Find(x => x.UserNumber == notif.User);
                if (userTag != null)
                {
                    notif.UserName = userTag.UserNumber + ": '" + userTag.UserName + "'";
                }
                else
                {
                    notif.UserName = notif.User == 0 ? "'maestro'" : "'" + notif.User + "'";
                }
            }
            catch
            {
                notif.UserName = notif.User.ToString();
            }
        }

        private void CreateEvent(NotificationRequest request, string body)
        {
            DateTime eventDatetime;
            try
            {
                eventDatetime = DateTime.ParseExact(request.EventDateTime, "dd-MM-yy HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                // IANA id, not the Windows "Argentina Standard Time" id -- this runs on Linux in
                // production, where FindSystemTimeZoneById only resolves IANA/tzdata names.
                var timeInfo = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
                eventDatetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeInfo);
            }
            var events = new List<Event>
            {
                new Event
                {
                    AlarmIdentifier = request.AlarmIdentifier,
                    AlarmParameter = request.AlarmParameter,
                    EventDateTime = eventDatetime,
                    EventType = request.EventType,
                    NotificationType = request.NotificationType,
                    Partition = request.Partition,
                    Secuence = request.Secuence,
                    Text = body
                }
            };
            _eventDao.CreateEvents(events);
        }

        public List<Event> EnumEvents(long deviceId) => _eventDao.EnumEvents(deviceId);
    }
}
