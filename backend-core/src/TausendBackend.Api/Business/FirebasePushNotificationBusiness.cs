using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using TausendBackend.Api.Dao;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Models.Notifications;

namespace TausendBackend.Api.Business
{
    public class FirebasePushNotificationBusiness : PushNotificationBusiness
    {
        /// <summary>
        /// Mutable OAuth token cache shared across every <see cref="FirebasePushNotificationBusiness"/>
        /// instance created during the app's lifetime. A single instance of this class is owned by the
        /// (singleton) <see cref="PushNotificationBusinessFactory"/> and handed to each short-lived
        /// FirebasePushNotificationBusiness it constructs -- that's what replaces the original's
        /// `static string _oauthToken` / `static DateTime _oauthTokenExpiryUtc` fields now that this
        /// class itself is no longer a process-wide singleton (it's re-created per notification, since
        /// it still takes the NotificationBase via its constructor like the original).
        /// </summary>
        public sealed class OAuthTokenCache
        {
            public string? Token;
            public DateTime ExpiryUtc;
        }

        // Was hardcoded to the "lomo-tausend" project (a name that only ever existed in this
        // comment/URL, not in any credentials file this app has actually had) -- built from
        // config now so it always matches whichever project Firebase:CredentialsPath's service
        // account actually belongs to.
        private const string DefaultProjectId = "alarmas-tausend-app";
        private const string DefaultCredentialsPath = "firebase-adminsdk.json";

        private string FcmUrl => $"https://fcm.googleapis.com/v1/projects/{_configuration["Firebase:ProjectId"] ?? DefaultProjectId}/messages:send";

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OAuthTokenCache _tokenCache;

        public FirebasePushNotificationBusiness(
            NotificationBase notification,
            IConfiguration configuration,
            HttpClient httpClient,
            IServiceScopeFactory scopeFactory,
            OAuthTokenCache tokenCache) : base(notification)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _scopeFactory = scopeFactory;
            _tokenCache = tokenCache;
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
            var response = await SendFcmRequestAsync(payload, notification.DeviceToken ?? "");
            return response;
        }

        private async Task<string> GetOAuthTokenAsync()
        {
            try
            {
                if (_tokenCache.Token != null && _tokenCache.ExpiryUtc > DateTime.UtcNow.AddMinutes(5))
                {
                    return _tokenCache.Token;
                }

                // Configurable so this works on the actual deployment target (Linux/Windows,
                // dev or prod) instead of the old backend's hardcoded dev-machine path
                // (E:\Codenmate\tausend.codenmate.com\lomo-tausend-firebase-adminsdk.json),
                // which was already wrong for the real production server.
                var credentialsPath = _configuration["Firebase:CredentialsPath"] ?? DefaultCredentialsPath;

                GoogleCredential credential;
                using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                }

                var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                _tokenCache.Token = token;
                _tokenCache.ExpiryUtc = DateTime.UtcNow.AddMinutes(50);
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

            return JsonSerializer.Serialize(payload);
        }

        private async Task<string> SendFcmRequestAsync(string payload, string deviceToken)
        {
            // Reusamos el HttpClient de larga vida provisto por PushNotificationBusinessFactory
            // en lugar de crear uno nuevo por request (evita el "Socket Exhaustion" que obligaba
            // a reiniciar el servidor en el backend anterior).

            // Configurar headers (Nota: Authorization no es thread-safe si cambia el token,
            // pero como el token está cacheado y compartido, es aceptable en este contexto).
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenCache.Token);

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
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
                    // AccountDao is a Scoped service (it depends on the Scoped SqlDbContext), but this
                    // class is created by the AddSingleton-registered PushNotificationBusinessFactory --
                    // resolving a Scoped service straight from a singleton would capture it for the
                    // app's lifetime, so a short-lived scope is created just for this DB call instead.
                    using var scope = _scopeFactory.CreateScope();
                    var dao = scope.ServiceProvider.GetRequiredService<AccountDao>();
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
