using TausendBackend.Api.Models.Notifications;

namespace TausendBackend.Api.Business
{
    /// <summary>
    /// Replaces the old backend's <c>Lazy&lt;PushNotificationBusinessFactory&gt;</c> static singleton
    /// with a proper AddSingleton-registered service. It's effectively stateless except for the FCM
    /// OAuth token cache (see <see cref="FirebasePushNotificationBusiness.OAuthTokenCache"/>) and the
    /// long-lived HttpClient, both of which are exactly the kind of state that's safe -- and desirable
    /// -- to keep on a singleton for the app's lifetime.
    /// </summary>
    public class PushNotificationBusinessFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        // Instancia de HttpClient de larga vida (una por app, no por request/notificación), para
        // evitar el "Socket Exhaustion" del backend anterior. Ver comentario en
        // FirebasePushNotificationBusiness.SendFcmRequestAsync.
        private readonly HttpClient _httpClient = new HttpClient();

        // Cache compartido del token OAuth de FCM entre todas las notificaciones enviadas durante
        // la vida de la aplicación -- ver FirebasePushNotificationBusiness.OAuthTokenCache.
        private readonly FirebasePushNotificationBusiness.OAuthTokenCache _oauthTokenCache = new();

        public PushNotificationBusinessFactory(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public PushNotificationBusiness CreateNotificationBusiness(NotificationBase notification)
        {
            return new FirebasePushNotificationBusiness(notification, _configuration, _httpClient, _scopeFactory, _oauthTokenCache);
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
