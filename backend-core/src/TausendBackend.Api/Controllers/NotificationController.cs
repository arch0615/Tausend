using Microsoft.AspNetCore.Mvc;
using TausendBackend.Api.Business;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Requests;
using TausendBackend.Api.Responses;
using TausendBackend.Api.Security;

namespace TausendBackend.Api.Controllers
{
    // Routes intentionally mirror the old WCF UriTemplates (/NotificationService/{Op}).
    [ApiController]
    [Route("NotificationService")]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationBusiness _notificationBusiness;
        private readonly NotificationBuilder _notificationBuilder;
        private readonly PushNotificationBusinessFactory _pushFactory;
        private readonly AccountBusiness _accountBusiness;
        private readonly DeviceBusiness _deviceBusiness;
        private readonly IConfiguration _configuration;

        public NotificationController(
            NotificationBusiness notificationBusiness,
            NotificationBuilder notificationBuilder,
            PushNotificationBusinessFactory pushFactory,
            AccountBusiness accountBusiness,
            DeviceBusiness deviceBusiness,
            IConfiguration configuration)
        {
            _notificationBusiness = notificationBusiness;
            _notificationBuilder = notificationBuilder;
            _pushFactory = pushFactory;
            _accountBusiness = accountBusiness;
            _deviceBusiness = deviceBusiness;
            _configuration = configuration;
        }

        /// <summary>Mirrors DeviceController/CommandController's own copy of this check. EnumEvents
        /// below had none of this until now -- any logged-in account could read any device's event
        /// history just by guessing its DeviceId, the same class of bug fixed elsewhere on Day 5 /
        /// Day 27.</summary>
        private long AuthorizeDeviceAccess(string accessToken, long deviceId, out bool hasAccess)
        {
            var accountId = _accountBusiness.ResolveAccountId(accessToken);
            if (accountId == 0)
            {
                hasAccess = false;
                return 0;
            }
            var owns = _deviceBusiness.GetDevice(deviceId, accountId).Device != null;
            hasAccess = owns || _accountBusiness.IsAdmin(accessToken);
            return accountId;
        }

        /// <summary>
        /// Sends a real push to a real handset, so it is gated twice: the caller must be signed
        /// in, and the target device token must be one registered to their own account (an Admin
        /// may target any). It used to require nothing at all -- no access token, no ownership --
        /// so anyone who learned a device token could push arbitrary notifications to that phone.
        /// On an alarm product that means a convincing fake "Panic" or "Fire" alert on someone
        /// else's handset, which is why this is worth closing even though the endpoint is only
        /// ever used for diagnostics.
        /// </summary>
        [HttpPost("SendTestNotification")]
        public CommandResponse SendTestNotification(SendTestNotificationRequest request)
        {
            var accountId = _accountBusiness.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var unauthorized = new CommandResponse();
                unauthorized.InformUnauthorized();
                return unauthorized;
            }

            // The token may carry an "android:"/"ios:" prefix (parsed below); compare on the bare
            // value so a prefixed request cannot sidestep the ownership check.
            var bare = request.DeviceToken.Contains(':')
                ? request.DeviceToken.Split(':', 2)[1]
                : request.DeviceToken;
            var ownsToken = _accountBusiness.EnumDeviceTokenOfAccount(accountId)
                .Any(t => string.Equals(t.Token, bare, StringComparison.Ordinal));
            if (!ownsToken && !_accountBusiness.IsAdmin(request.AccessToken))
            {
                var forbidden = new CommandResponse();
                forbidden.InformForbidden();
                return forbidden;
            }

            string response;
            try
            {
                var os = PhoneOS.Android;
                var token = request.DeviceToken;
                if (!string.IsNullOrWhiteSpace(request.DeviceToken) && request.DeviceToken.Contains(':'))
                {
                    var parts = request.DeviceToken.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var osHint = parts[0].Trim().ToLower();
                        if (osHint is "ios" or "apple" or "apns")
                            os = PhoneOS.iOS;
                        else if (osHint == "android")
                            os = PhoneOS.Android;
                        token = parts[1];
                    }
                }

                _notificationBuilder.DeviceToken = token;
                _notificationBuilder.NotificationType = NotificationTypes.Test;
                _notificationBuilder.OS = os;
                var notif = _notificationBuilder.Build();
                response = _pushFactory.CreateNotificationBusiness(notif).SendPushNotification();
            }
            catch (Exception e)
            {
                response = e.Message;
            }
            return new CommandResponse { Text = response };
        }

        // Relay-only (Tausend.UDPListener) -- panel event notifications, including real alarms.
        // No user AccessToken concept applies here; gated on the shared RelaySystemKey instead --
        // see backend/RELAY_DECISION.md and DAY5_SUMMARY.md.
        [HttpPost("NotifyEvent")]
        public CommandResponse NotifyEvent(NotificationRequest request)
        {
            if (!SystemAuth.IsValidRequest(Request, _configuration))
            {
                var unauthorized = new CommandResponse();
                unauthorized.InformUnauthorized();
                return unauthorized;
            }
            return new CommandResponse { Text = _notificationBusiness.ProcessNotificationRequest(request) };
        }

        // Client, 27 Oct: "Apagar notificaciones" on Gestionar central -- the caller must own the
        // panel (same IDOR protection as everything else keyed by Identifier), but muting is scoped
        // to the DeviceToken the app itself sends, not the account, so this phone's mute never
        // reaches any other phone or account sharing the panel.
        [HttpPost("SetNotificationMute")]
        public SimpleResponse SetNotificationMute(SetNotificationMuteRequest request)
        {
            var accountId = _accountBusiness.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var res = new SimpleResponse();
                res.InformUnauthorized();
                return res;
            }
            if (_deviceBusiness.GetDevice(request.Identifier, accountId).Device == null)
            {
                var res = new SimpleResponse();
                res.InformNotFound($"Dispositivo {request.Identifier} no encontrado");
                return res;
            }
            _notificationBusiness.SetNotificationMute(request.DeviceToken, request.Identifier, request.Muted);
            return new SimpleResponse();
        }

        [HttpPost("GetNotificationMute")]
        public NotificationMuteResponse GetNotificationMute(GetNotificationMuteRequest request)
        {
            var accountId = _accountBusiness.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var res = new NotificationMuteResponse();
                res.InformUnauthorized();
                return res;
            }
            if (_deviceBusiness.GetDevice(request.Identifier, accountId).Device == null)
            {
                var res = new NotificationMuteResponse();
                res.InformNotFound($"Dispositivo {request.Identifier} no encontrado");
                return res;
            }
            return new NotificationMuteResponse { Muted = _notificationBusiness.IsNotificationMuted(request.DeviceToken, request.Identifier) };
        }

        [HttpPost("EnumEvents")]
        public ListOfEventsResponse EnumEvents(CommandRequest request)
        {
            var accountId = AuthorizeDeviceAccess(request.AccessToken, request.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new ListOfEventsResponse(null);
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new ListOfEventsResponse(null);
                res.InformNotFound($"Dispositivo {request.DeviceId} no encontrado");
                return res;
            }
            return new ListOfEventsResponse(_notificationBusiness.EnumEvents(request.DeviceId));
        }
    }
}
