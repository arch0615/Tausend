using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Tausend.Core.Business;
using Tausend.Core.Entities.Notifications;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Factories;
using Tausend.Core.Models;
using Tausend.Core.Responses;
using Tausend.Core.Security;

namespace Tausend.Core.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "NotificationService" en el código, en svc y en el archivo de configuración a la vez.
    // NOTA: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione NotificationService.svc o NotificationService.svc.cs en el Explorador de soluciones e inicie la depuración.
    public class NotificationService : INotificationService
    {
        public CommandResponse SendTestNotification(String DeviceToken)
        {
            string response;
            try
            {
                // Allow passing "ios:<token>" or "android:<token>" to pick platform when testing
                var os = Enums.PhoneOS.Android;
                var token = DeviceToken;
                if (!string.IsNullOrWhiteSpace(DeviceToken) && DeviceToken.Contains(":"))
                {
                    var parts = DeviceToken.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var osHint = parts[0].Trim().ToLower();
                        if (osHint == "ios" || osHint == "apple" || osHint == "apns")
                            os = Enums.PhoneOS.iOS;
                        else if (osHint == "android")
                            os = Enums.PhoneOS.Android;
                        token = parts[1];
                    }
                }

                var notifBuilder = new NotificationBuilder();
                notifBuilder.DeviceToken = token;
                notifBuilder.NotificationType = Enums.NotificationTypes.Test;
                notifBuilder.OS = os;
                var notif = notifBuilder.Build();
                var bz = PushNotificationBusinessFactory.Instance.CreateNotificationBusiness(notif);
                response = bz.SendPushNotification();
            }
            catch (Exception e)
            {
                response = e.Message;
            }
            return new CommandResponse() { Text = response };
        }
        // Relay-only endpoint (panel event notifications) -- no user AccessToken concept
        // applies here, so it's gated on the shared RelaySystemKey instead. See
        // Tausend.Core.Security.SystemAuth and backend/RELAY_DECISION.md.
        public CommandResponse NotifyEvent(NotificationRequest request)
        {
            if (!SystemAuth.IsValidRequest())
            {
                var unauthorized = new CommandResponse();
                unauthorized.InformUnauthorized();
                return unauthorized;
            }
            string response;
            try
            {
                var notifBz = new NotificationBusiness();
                response = notifBz.ProcessNotificationRequest(request);
            }
            catch (Exception e)
            {
                response = e.Message;
            }
            return new CommandResponse() { Text = response };
        }

        public ListOfEventsResponse EnumEvents(CommandRequest request)
        {
            var res = new ListOfEventsResponse(null);
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(request.AccessToken))
            {
                res.InformUnauthorized();
                return res;
            }
            try
            {
                res = new ListOfEventsResponse(new NotificationBusiness().EnumEvents(request.DeviceId));
            }
            catch (Exception e)
            {
                res.Code = 500;
                res.Message = e.Message;
            }
            return res;
        }
    }
}
