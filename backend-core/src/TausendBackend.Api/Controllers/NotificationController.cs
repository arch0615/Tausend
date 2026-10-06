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

        [HttpPost("SendTestNotification")]
        public CommandResponse SendTestNotification(SendTestNotificationRequest request)
        {
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
