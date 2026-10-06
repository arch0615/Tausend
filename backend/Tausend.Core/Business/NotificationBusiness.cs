using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Models;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Enums;
using Tausend.Core.Factories;
using Tausend.Core.Models;

namespace Tausend.Core.Business
{
    public class NotificationBusiness
    {
        public string SendEmergencyNotification(long deviceId)
        {
            try
            {
                AccountBusiness accountBz = new AccountBusiness();
                DeviceBusiness deviceBz = new DeviceBusiness();
                // System lookup (notifying every owner of the device), not scoped to one account.
                var device = deviceBz.GetDevice(deviceId, 0);
                var owners = accountBz.EnumOwnersOfDevice(device.Device.Identifier);
                foreach (var account in owners)
                {
                    var deviceTokens = accountBz.EnumDeviceTokenOfAccount(account.AccountId);
                    foreach (var item in deviceTokens)
                    {
                        var notifBuilder = new NotificationBuilder()
                        {
                            NotificationType = NotificationTypes.Medical,
                            DeviceToken = item.Token,
                            OS = item.OS
                        };
                        var notif = notifBuilder.Build();
                        notif.DeviceDescription = device.Device.Description;
                        var pushNotifBz = PushNotificationBusinessFactory.Instance.CreateNotificationBusiness(notif);
                        pushNotifBz.SendPushNotification();
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
                AccountBusiness accountBz = new AccountBusiness();
                DeviceBusiness deviceBz = new DeviceBusiness();
                var owners = accountBz.EnumOwnersOfDevice(request.AlarmIdentifier);
                var body = "";
                foreach (var account in owners)
                {
                    //if (request.AlarmParameter == "000")
                    //{
                    //    deviceBz.DeviceDisassociate(request.AlarmIdentifier, account.AccountId)
                    //}
                    var deviceTokens = accountBz.EnumDeviceTokenOfAccount(account.AccountId);
                    foreach (var item in deviceTokens)
                    {
                        var notifBuilder = new NotificationBuilder()
                        {
                            NotificationType = (NotificationTypes)request.NotificationType,
                            AlarmParameter = request.AlarmParameter,
                            EventType = request.EventType,
                            DeviceToken = item.Token,
                            OS = item.OS,
                            Icon = "notification_icon",
                            Sound = "default",
                            AlarmIdentifier = request.AlarmIdentifier,
                            AccountId = account.AccountId,
                            Date = request.EventDateTime,
                            Email = request.Email
                        };
                        var notif = notifBuilder.Build();
                        SetUserName(notif, request.AlarmIdentifier, account.AccountId);
                        var pushNotifBz = PushNotificationBusinessFactory.Instance.CreateNotificationBusiness(notif);
                        pushNotifBz.SendPushNotification();
                        body = notif.Body;
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

        public void SetUserName(Entities.Notifications.NotificationBase notif, string identifier, long accountId)
        {
            try
            {
                var devBz = new DeviceBusiness();
                var device = devBz.GetDevice(identifier, accountId);
                notif.DeviceDescription = device.Device.Description;
                if (!notif.UseUserParam) return;
                var usBz = new UserBusiness();
                var users = usBz.EnumUsers(device.Device.DeviceId);
                var userTag = users.Find(x => x.UserNumber == notif.User);
                if (userTag != null)
                {
                    notif.UserName = userTag.UserNumber + ": '" + userTag.UserName + "'";
                }
                else
                {
                    if (notif.User == 0)
                        notif.UserName = "'maestro'";
                    else
                        notif.UserName = "'" + notif.User.ToString() + "'";
                }
            }
            catch
            {
                notif.UserName = notif.User.ToString();
            }
        }

        private void CreateEvent(NotificationRequest request, string body)
        {
            var events = new List<Event>();
            DateTime eventDatetime;
            try
            {
                eventDatetime = DateTime.ParseExact(request.EventDateTime, "dd-MM-yy HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                var timeInfo = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
                eventDatetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeInfo);
            }
            events.Add(new Event()
            {
                AlarmIdentifier = request.AlarmIdentifier,
                AlarmParameter = request.AlarmParameter,
                EventDateTime = eventDatetime,
                EventType = request.EventType,
                NotificationType = request.NotificationType,
                Partition = request.Partition,
                Secuence = request.Secuence,
                Text = body
            });
            var evDao = new EventDao();
            evDao.CreateEvents(events);
        }

        public List<Event> EnumEvents(long deviceId)
        {
            var dao = new EventDao();
            return dao.EnumEvents(deviceId);
        }
    }
}