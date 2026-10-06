using System.Text.Json;
using System.Text.Json.Serialization;
using TausendRelay.Security;

namespace TausendRelay.Business
{
    public record DeviceInfo(
        [property: JsonPropertyName("DeviceId")] long DeviceId,
        [property: JsonPropertyName("Identifier")] string? Identifier,
        [property: JsonPropertyName("PublicKey")] ushort PublicKey);

    /// <summary>
    /// Relay -> backend leg of the command path (port of
    /// backend/Tausend.UDPListener/Business/ServiceBusiness.cs). Talks to backend-core's
    /// DeviceService/NotificationService controllers directly (no per-service URLs like the
    /// original -- those all pointed at the same backend anyway, just different WCF .svc
    /// endpoints that don't exist in this form anymore).
    ///
    /// Two bugs fixed from the original during this port:
    /// - Execute() no longer sets fields on a null response inside its own catch block when the
    ///   HTTP call itself throws (a guaranteed NullReferenceException masking the real failure --
    ///   flagged but left unfixed in RELAY_DECISION.md).
    /// - DisassociateDevice() now calls the correctly-spelled "DisassociateCentral" route --
    ///   the original had a "DissasociateCentral" typo that silently 404'd forever.
    /// </summary>
    public class BackendClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackendClient> _logger;

        public BackendClient(HttpClient httpClient, IConfiguration configuration, ILogger<BackendClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public Task<DeviceInfo?> GetDeviceAsync(long deviceId) =>
            PostAsync<DeviceInfo>("DeviceService/GetDeviceByID", new { DeviceId = deviceId }, "Device");

        public Task<DeviceInfo?> GetDeviceAsync(string identifier) =>
            PostAsync<DeviceInfo>("DeviceService/GetDeviceByIdentifier", new { Identifier = identifier }, "Device");

        public Task NotifyEventAsync(string identifier, int parameter, string dateTime, string eventType, int cid, int partition, int sequence) =>
            ExecuteAsync("NotificationService/NotifyEvent", new
            {
                AlarmIdentifier = identifier,
                AlarmParameter = parameter,
                EventDateTime = dateTime,
                EventType = eventType,
                NotificationType = cid,
                Partition = partition,
                Secuence = sequence,
            });

        public Task DisassociateDeviceAsync(string identifier) =>
            ExecuteAsync("DeviceService/DisassociateCentral", new { Identifier = identifier });

        public Task UpdateDeviceLastConnectionAsync(long deviceId) =>
            ExecuteAsync("DeviceService/UpdateDeviceLastConnection", new { DeviceId = deviceId });

        private async Task<T?> PostAsync<T>(string path, object body, string wrapperProperty) where T : class
        {
            var content = await ExecuteAsync(path, body);
            if (content is null)
                return null;
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty(wrapperProperty, out var element) || element.ValueKind == JsonValueKind.Null)
                    return null;
                return element.Deserialize<T>();
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "Could not parse {Path} response", path);
                return null;
            }
        }

        private async Task<string?> ExecuteAsync(string path, object body)
        {
            try
            {
                var baseUrl = _configuration["BackendBaseUrl"]
                    ?? throw new InvalidOperationException("Missing BackendBaseUrl config");
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{path}")
                {
                    Content = JsonContent.Create(body),
                };
                request.Headers.Add(SystemAuth.HeaderName, _configuration["RelaySystemKey"]);
                var response = await _httpClient.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Backend call to {Path} failed", path);
                return null;
            }
        }
    }
}
