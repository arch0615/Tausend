using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tausend.Core.Dao;
using Tausend.Core.Entities.Notifications;
using Tausend.Core.Enums;
using Tausend.Core.Models;

namespace Tausend.Core.Business
{
    public class FirebasePushNotificationBusiness : PushNotificationBusiness
    {
        private static readonly string FcmUrl = "https://fcm.googleapis.com/v1/projects/lomo-tausend/messages:send";
        private static string _oauthToken;
        private static DateTime _oauthTokenExpiryUtc;

        // CAMBIO CRÍTICO 1: Instancia estática de HttpClient.
        // Esto evita el "Socket Exhaustion" que obligaba a reiniciar el servidor.
        // Se mantiene viva durante toda la vida de la aplicación.
        private static readonly HttpClient _httpClient = new HttpClient();

        public FirebasePushNotificationBusiness(NotificationBase notification) : base(notification)
        {
        }

        protected override void GetNotificationSettings()
        {
        }

        protected override string SendNotification(NotificationBase notification)
        {
            return SendNotificationAsync(notification).GetAwaiter().GetResult();
        }

        private async Task<string> SendNotificationAsync(NotificationBase notification)
        {
            await GetOAuthTokenAsync();
            var payload = CreatePayload(notification);
            var response = await SendFcmRequestAsync(payload, notification.DeviceToken);
            return response;
        }

        private async Task<string> GetOAuthTokenAsync()
        {
            try
            {
                if (_oauthToken != null && _oauthTokenExpiryUtc > DateTime.UtcNow.AddMinutes(5))
                {
                    return _oauthToken;
                }

                GoogleCredential credential;
                using (var stream = new FileStream(@"E:\Codenmate\tausend.codenmate.com\lomo-tausend-firebase-adminsdk.json", FileMode.Open, FileAccess.Read))            
                {
                    credential = GoogleCredential.FromStream(stream)
                                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                }

                var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                _oauthToken = token;
                _oauthTokenExpiryUtc = DateTime.UtcNow.AddMinutes(50);
                return token;
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error obtaining OAuth token for FCM: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determina si el tipo de notificación corresponde a una alarma real
        /// que justifica sonar sirena en el dispositivo del usuario.
        /// </summary>
        private static bool IsAlarmNotification(NotificationTypes type)
        {
            switch (type)
            {
                case NotificationTypes.Medical:
                case NotificationTypes.PersonalMedical:
                case NotificationTypes.Fire:
                case NotificationTypes.Panic:
                case NotificationTypes.Assault:
                case NotificationTypes.SilentPanic:
                case NotificationTypes.KeypadAssault:
                case NotificationTypes.Stole:
                case NotificationTypes.Sabotage:
                case NotificationTypes.ZoneCross:
                case NotificationTypes.SilentAlarm:
                case NotificationTypes.Gas:
                    return true;
                default:
                    return false;
            }
        }

        private string CreatePayload(NotificationBase notification)
        {
            var isAlarm = IsAlarmNotification(notification.Type);

            // Android: canal con sirena solo para alarmas reales
            var androidChannelId = isAlarm ? "my_channel_id" : "default_channel_id";
            var androidSound = isAlarm ? "alert" : "default";
            var androidPriority = isAlarm ? "high" : "normal";
            var androidNotificationPriority = isAlarm ? "PRIORITY_MAX" : "PRIORITY_DEFAULT";

            // iOS: ROLLOUT PHASE 1 — see PUSH_ROLLOUT.md
            // Forcing "default" for ALL iOS notifications (including alarms)
            // until the new iOS app that bundles alert.caf is widely deployed.
            // Old iOS clients do not have alert.caf in their bundle; sending it
            // risks silent alarms on those installs.
            // TODO(phase-2): once the new iOS build is live in the App Store
            // and has propagated to users, change this line back to:
            //     var iosSound = isAlarm ? "alert.caf" : "default";
            // See PUSH_ROLLOUT.md for the exact flip procedure and test plan.
            var iosSound = "default";
            var apnsPriority = isAlarm ? "10" : "5";

            var apnsHeaders = new Dictionary<string, string>
            {
                { "apns-priority", apnsPriority },
                { "apns-push-type", "alert" }
            };

            var apsPayload = new Dictionary<string, object>
            {
                { "alert", new { title = notification.Title, body = notification.Body } },
                { "badge", 1 },
                { "sound", iosSound },
                { "content-available", 1 },
                { "mutable-content", 1 }
            };

            var payload = new
            {
                message = new
                {
                    token = notification.DeviceToken,
                    data = new
                    {
                        body = notification.Body,
                        title = notification.Title,
                        badge = "1",
                        icon = notification.Icon ?? "ic_notification",
                        sound = androidSound,
                        vibrate = isAlarm ? "true" : "false",
                        android_channel_id = androidChannelId,
                        click_action = "OPEN_ACTIVITY",
                        type = notification.Type.ToString()
                    },
                    notification = new
                    {
                        body = notification.Body,
                        title = notification.Title
                    },
                    android = new
                    {
                        priority = androidPriority,
                        notification = new
                        {
                            channel_id = androidChannelId,
                            sound = androidSound,
                            default_vibrate_timings = isAlarm,
                            default_sound = !isAlarm,
                            notification_priority = androidNotificationPriority
                        }
                    },
                    apns = new
                    {
                        headers = apnsHeaders,
                        payload = new
                        {
                            aps = apsPayload
                        }
                    }
                }
            };

            return JsonConvert.SerializeObject(payload);
        }

        private async Task<string> SendFcmRequestAsync(string payload, string deviceToken)
        {
            // CAMBIO: Usamos _httpClient estático en lugar de 'using (var client = new HttpClient())'

            // Configurar headers (Nota: Authorization no es thread-safe si cambia el token, 
            // pero como _oauthToken es global y cacheado, es aceptable en este contexto).
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthToken);

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                // Usamos la instancia estática
                var response = await _httpClient.PostAsync(FcmUrl, content);

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode ||
                    responseString.IndexOf("UNREGISTERED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    responseString.IndexOf("NotRegistered", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    HandleUnregisteredToken(deviceToken, response.StatusCode, responseString);
                }

                Trace.WriteLine($"FCM response ({response.StatusCode}) for token {deviceToken}: {responseString}");
                return $"StatusCode: {response.StatusCode}, Body: {responseString}";
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error sending FCM request: {ex.Message}");
                // Es importante no romper el flujo completo si falla un envío HTTP
                return $"Error: {ex.Message}";
            }
        }

        private void HandleUnregisteredToken(string deviceToken, HttpStatusCode statusCode, string responseString)
        {
            try
            {
                if (statusCode == HttpStatusCode.NotFound ||
                    responseString.IndexOf("UNREGISTERED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    responseString.IndexOf("NotRegistered", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var dao = new AccountDao();
                    dao.DeleteDeviceToken(deviceToken);
                    Trace.WriteLine($"DeviceToken removed due to UNREGISTERED/NotRegistered: {deviceToken}");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error removing invalid DeviceToken {deviceToken}: {e.Message}");
            }
        }
    }
}