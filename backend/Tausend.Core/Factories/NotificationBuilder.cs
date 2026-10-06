using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tausend.Core.Business;
using Tausend.Core.Entities.Notifications;
using Tausend.Core.Enums;
using Tausend.Core.Models;

namespace Tausend.Core.Factories
{
    public class NotificationBuilder
    {
        public NotificationTypes NotificationType { get; set; }
        public PhoneOS OS { get; set; }
        public string Icon { get; set; }
        public string DeviceToken { get; set; }
        public int AlarmParameter { get; set; }
        public string EventType { get; set; }
        public string Sound { get; set; }
        public string AlarmIdentifier { get; set; }
        public long AccountId { get; set; }
        public string Date { get; set; }
        public string Email { get; set; }
        public NotificationBuilder()
        {
            NotificationType = NotificationTypes.Unknown;
            OS = PhoneOS.Unknown;
        }

        public NotificationBase Build()
        {
            ValidateNotificationValues();
            var result = CreateNotificationType();
            SetNotificationValues(result);
            return result;
        }

        private void ValidateNotificationValues()
        {
            if (NotificationType == NotificationTypes.Unknown)
            {
                throw new Exception("CreateNotification: Falta el NotificationType");
            }
            if (OS == PhoneOS.Unknown)
            {
                throw new Exception("CreateNotification: Falta el PhoneTypes");
            }
            if (string.IsNullOrWhiteSpace(DeviceToken))
            {
                throw new Exception("CreateNotification: Falta el DeviceToken");
            }
        }

        private void SetNotificationValues(NotificationBase result)
        {
            result.DeviceToken = DeviceToken;
            result.Icon = Icon;
            result.OS = OS;
            result.EventType = EventType;
            result.Sound = Sound;
            result.Email = Email;
            if (result.UseDualParam)
            {
                if (result.UseZoneParam)
                    result.Zone = AlarmParameter;
                else if (result.UseKeypadParam)
                    result.Keypad = AlarmParameter;
                else if (result.UseUserParam)
                {
                    result.User = AlarmParameter;
                    SetUserName(result);
                }
            }
            else if (result.UseZoneParam)
            {
                result.Zone = AlarmParameter;
            }
            else if (result.UseKeypadParam)
            {
                result.Keypad = AlarmParameter;
            }
            else if (result.UseUserParam)
            {
                result.User = AlarmParameter;
                SetUserName(result);
            }
            else if (result.UsePointParam)
            {
                result.Point = AlarmParameter;
            }
            result.Date = Date;
        }

        private void SetUserName(NotificationBase result)
        {
            if (!result.UseUserParam)
                return;
            var userBz = new UserBusiness();
            string userName;
            // if (AlarmParameter == 0) 
            //      userName = "maestro"; 
            // else
            userName = userBz.GetUserTag(AccountId, AlarmIdentifier, AlarmParameter);
            result.UserName = userName;
        }

        private NotificationBase CreateNotificationType()
        {
            NotificationBase result = null;
            switch (NotificationType)
            {
                case NotificationTypes.Unknown:
                    result = new UnknownEventNotification();
                    break;
                case NotificationTypes.Test:
                    result = new TestNotification();
                    break;
                case NotificationTypes.Medical:
                    result = new MedicalNotification();
                    break;
                case NotificationTypes.PersonalMedical:
                    result = new PersonalMedicalNotification();
                    break;
                case NotificationTypes.Fire:
                    result = new FireNotification();
                    break;
                case NotificationTypes.Panic:
                    result = new PanicNotification();
                    break;
                case NotificationTypes.Assault:
                    result = new AssaultNotification();
                    break;
                case NotificationTypes.SilentPanic:
                    result = new SilentPanicNotification();
                    break;
                case NotificationTypes.KeypadAssault:
                    result = new KeypadAssaultNotification();
                    break;
                case NotificationTypes.Stole:
                    result = new StoleNotification();
                    break;
                case NotificationTypes.Sabotage:
                    result = new SabotageNotification();
                    break;
                case NotificationTypes.ZoneCross:
                    result = new ZoneCrossNotification();
                    break;
                case NotificationTypes.Gas:
                    result = new GasNotification();
                    break;
                case NotificationTypes.SilentAlarm:
                    result = new SilentAlarm();
                    break;
                case NotificationTypes.InvalidFail:
                    result = new InvalidFailNotification();
                    break;
                case NotificationTypes.VACLineFail:
                    result = new VACLineFailNotification();
                    break;
                case NotificationTypes.BatteryFail:
                    result = new BatteryFailNotification();
                    break;
                case NotificationTypes.Program:
                    result = new ProgramNotification();
                    break;
                case NotificationTypes.Siren1Fail:
                    result = new Siren1FailNotification();
                    break;
                case NotificationTypes.Siren2Fail:
                    result = new Siren2FailNotification();
                    break;
                case NotificationTypes.BusFail:
                    result = new BusFailNotification();
                    break;
                case NotificationTypes.Auxiliar12VFail:
                    result = new Auxiliar12VFail();
                    break;
                case NotificationTypes.PhoneLineFail:
                    result = new PhoneLineFailNotification();
                    break;
                case NotificationTypes.CommunicatorFail:
                    result = new CommunicatorFail();
                    break;
                case NotificationTypes.UserArmDisarm:
                    result = new UserArmDisarm();
                    break;
                case NotificationTypes.AutoArm:
                    result = new AutoArm();
                    break;
                case NotificationTypes.AutoArmCanceled:
                    result = new AutoArmCanceledNotification();
                    break;
                case NotificationTypes.BellDisarm:
                    result = new BellDisarmNotification();
                    break;
                case NotificationTypes.FastArm:
                    result = new FastArmNotification();
                    break;
                case NotificationTypes.KeyArmDisarm:
                    result = new KeyArmDisarmNotification();
                    break;
                case NotificationTypes.UserAccessControl:
                    result = new UserAccessControlNotification();
                    break;
                case NotificationTypes.PointAccessControl:
                    result = new PointAccessControlNotification();
                    break;
                case NotificationTypes.AutoArmFail:
                    result = new AutoArm();
                    break;
                case NotificationTypes.PartArm:
                    result = new PartArmNotification();
                    break;
                case NotificationTypes.MemoryDisarmed:
                    result = new MemoryDisarmedNotification();
                    break;
                case NotificationTypes.RecentArmedAlarm:
                    result = new RecentArmedAlarmNotification();
                    break;
                case NotificationTypes.ZoneBypass:
                    result = new ZoneBypassNotification();
                    break;
                case NotificationTypes.ManualTest:
                    result = new ManualTestNotification();
                    break;
                case NotificationTypes.PeriodicTest:
                    result = new PeriodicTestNotification();
                    break;
                case NotificationTypes.ClockFail:
                    result = new ClockFailNotification();
                    break;
                case NotificationTypes.UnknownEvent:
                    result = new UnknownEventNotification();
                    break;
                case NotificationTypes.ServerComunicationFail:
                    result = new ServerComunicationFail();
                    break;
                case NotificationTypes.UserLink3Fail:
                    result = new UserLink3FailNotification();
                    break;
                case NotificationTypes.UserLinked:
                    result = new UserLinkedNotification();
                    break;
                default:
                    result = new UnknownEventNotification();
                    break;
            }
            if (result == null)
                throw new NotImplementedException("NotificationType not Implemented");
            return result;
        }
    }
}