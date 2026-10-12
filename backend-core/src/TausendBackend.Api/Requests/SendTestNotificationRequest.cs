namespace TausendBackend.Api.Requests
{
    public class SendTestNotificationRequest
    {
        /// <summary>Required. This endpoint sends a real push to a real handset, so it is gated
        /// the same way every other user-facing endpoint is rather than being open.</summary>
        public string AccessToken { get; set; } = "";

        public string DeviceToken { get; set; } = "";
    }
}
