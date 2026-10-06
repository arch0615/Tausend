using System.Globalization;
using System.Text.Json;
using TausendBackend.Api.Dao;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Requests;
using TausendBackend.Api.Responses;
using TausendBackend.Api.Security;

namespace TausendBackend.Api.Business
{
    /// <summary>
    /// Client for the relay's PrivateService (Tausend.UDPListener) -- the backend->relay leg of
    /// the command path described in ../backend/RELAY_DECISION.md. Behaviorally mirrors
    /// Tausend.Core.Business.CommandBusiness, including its raw-string-Contains response
    /// parsing (GetStatus/GetVersion/etc. below): the relay's JSON responses are WCF "Wrapped"
    /// body style (e.g. {"SendCommandResult":{"Text":"ARM,STAY",...}}), and the original parses
    /// them by substring-matching the raw response body rather than deserializing first -- that
    /// hacky-looking behavior is preserved deliberately for parity, not fixed here.
    /// </summary>
    public class CommandBusiness
    {
        private readonly AccountBusiness _accountBusiness;
        private readonly DeviceDao _deviceDao;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // DeviceDao, not DeviceBusiness -- DeviceBusiness itself depends on CommandBusiness, so
        // taking a DeviceBusiness dependency here would be a circular constructor reference the
        // DI container can't resolve. DeviceDao is a leaf dependency (SqlDbContext only).
        public CommandBusiness(AccountBusiness accountBusiness, DeviceDao deviceDao, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _accountBusiness = accountBusiness;
            _deviceDao = deviceDao;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> ArmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.ARM + "A", commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> DayArmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.ARM + "S", commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> DisarmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.DISARM, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> NightArmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.ARM + "N", commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> Exclusion(ExclusionRequest commandRequest)
        {
            try
            {
                var device = _deviceDao.GetDevice(commandRequest.DeviceId, 0);
                var pin = device != null ? _deviceDao.GetDevicePIN(commandRequest.DeviceId) : null;
                var body = new { cmd = COMMANDS.BYPASS, identifier = device?.Identifier, pin, zones = commandRequest.Zones };
                var res = await ExecuteAsync("BYPCommand", body);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> PGMCommand(PGMRequest commandRequest)
        {
            try
            {
                var device = _deviceDao.GetDevice(commandRequest.DeviceId, 0);
                var pin = device != null ? _deviceDao.GetDevicePIN(commandRequest.DeviceId) : null;
                var body = new { cmd = COMMANDS.PROGRAM_CONTROL, identifier = device?.Identifier, pin, zone = commandRequest.Zone, state = commandRequest.State };
                await ExecuteAsync("PGMCommand", body);
                return "OK";
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> GetFirmwareVersion(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.VERSION, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetVersion(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> GetGeneralStatus(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> GetFailStatus(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.FAIL_STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var content = await ExecuteAsync("SendCommand", body);
                if (content.Contains("STSF:"))
                {
                    var subs = content.Substring(content.IndexOf("STSF:") + 5);
                    subs = subs.Remove(subs.IndexOf('"'));
                    return subs;
                }
                return GetStatus(content);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> GetZonesExclusion(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.BYPASS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetByPassResponse(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> GetZonesStatus(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.ZONES_STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetZonesResponse(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<string> GetMemoryStatus(CommandRequest commandRequest)
        {
            try
            {
                var body = CreateBody(COMMANDS.MEMORY_STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetZonesListResponse(res, COMMANDS.MEMORY_STATUS);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task ConfigurePublicKey(ushort key, string accessToken, long deviceId)
        {
            var hex = key.ToString("X4");
            await SendCommand("PGR059:" + hex, accessToken, deviceId);
            await SendCommand("PGR059:2,5,8", accessToken, deviceId);
        }

        public async Task<string> SendCommand(string command, string accessToken, long deviceId)
        {
            try
            {
                var body = CreateBody(command, accessToken, deviceId);
                var res = await ExecuteAsync("SendCommand", body);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public async Task<bool> SendGetPGMCommand(string accessToken, long deviceId, int pgm)
        {
            var command = "PGM" + pgm;
            var body = CreateBody(command, accessToken, deviceId);
            var wrapped = await ExecuteWrappedAsync(body);
            return (wrapped?.Text ?? "").Contains("PGM" + pgm + ":1");
        }

        public async Task<BatteryStateResponse> SendGetBatteryStatusCommand(string accessToken, long deviceId)
        {
            var response = new BatteryStateResponse();
            var body = CreateBody("STSB", accessToken, deviceId);
            var wrapped = await ExecuteWrappedAsync(body);
            var text = wrapped?.Text ?? "";
            if (text.Contains(':') && text.Contains(','))
            {
                text = text.Split(':')[1];
                var fields = text.Split(',');
                response.InTension = GetBatteryField(fields[0]);
                response.ChargeTension = GetBatteryField(fields[1]);
                response.TestTension = GetBatteryField(fields[2]);
                response.Current = float.Parse(fields[3].Substring(1), CultureInfo.InvariantCulture);
            }
            return response;
        }

        private static float GetBatteryField(string text) => float.Parse(text.Substring(1), CultureInfo.InvariantCulture) / 10;

        public async Task<string> SendInstallerCommand(string command, string accessToken, long deviceId)
        {
            var body = CreateBody(command, accessToken, deviceId);
            var wrapped = await ExecuteWrappedAsync(body);
            return wrapped?.Text ?? "";
        }

        public async Task<string> SendCommandToIdentifier(string command, string identifier, string pin = "")
        {
            command = "INST-" + command;
            var body = new { cmd = command, identifier, pin };
            var wrapped = await ExecuteWrappedAsync(body);
            return wrapped?.Text ?? "";
        }

        public async Task<DateTime> GetTimeCommand(CommandRequest commandRequest)
        {
            var body = CreateBody(COMMANDS.CLOCK, commandRequest.AccessToken, commandRequest.DeviceId);
            var wrapped = await ExecuteWrappedAsync(body);
            var timeText = (wrapped?.Text ?? "").Remove(0, 4).Trim();
            return DateTime.ParseExact(timeText, "HHmmss,ddMMyy", CultureInfo.InvariantCulture);
        }

        public async Task<DateTime> SyncCommand(CommandRequest commandRequest)
        {
            var serverTime = GetCurrentDateTime();
            var cmd = COMMANDS.CLOCK + ":" + serverTime.ToString("HHmmss,ddMMyy", CultureInfo.InvariantCulture);
            var body = CreateBody(cmd, commandRequest.AccessToken, commandRequest.DeviceId);
            var wrapped = await ExecuteWrappedAsync(body);
            var timeText = (wrapped?.Text ?? "").Remove(0, 4).Trim();
            return DateTime.ParseExact(timeText, "HHmmss,ddMMyy", CultureInfo.InvariantCulture);
        }

        public DateTime GetCurrentDateTime() => DateTime.Now.AddHours(-3);

        // Unscoped by DeviceId (accountId 0, the same "system caller" convention GetDevice uses
        // for the relay) rather than looked up via the caller's own account.Devices -- ownership
        // (or Admin status) is already verified by the calling Controller before any of these
        // methods run, so re-deriving the device from the caller's own device list here just
        // means it silently resolves to nothing (null identifier/pin sent to the relay) for any
        // caller acting on a device that isn't literally their own -- e.g. an Admin viewing
        // another account's panel. accessToken is unused now but kept in the signature since
        // every call site already threads it through and it costs nothing to leave in place.
        private object CreateBody(string command, string accessToken, long deviceId)
        {
            var device = _deviceDao.GetDevice(deviceId, 0);
            var pin = device != null ? _deviceDao.GetDevicePIN(deviceId) : null;
            return new { cmd = command, identifier = device?.Identifier, pin };
        }

        /// <summary>Raw response body -- callers that parse via GetStatus/GetVersion/etc. want the
        /// unparsed WCF-wrapped JSON text, matching the original's Contains-based parsing.</summary>
        private async Task<string> ExecuteAsync(string methodName, object body)
        {
            try
            {
                var baseUrl = _configuration["PrivateService"]
                    ?? throw new InvalidOperationException("Missing PrivateService config");
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{methodName}")
                {
                    Content = JsonContent.Create(body)
                };
                request.Headers.Add(SystemAuth.HeaderName, _configuration["RelaySystemKey"]);
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private class SendCommandResult
        {
            public string? Text { get; set; }
        }

        private class SendCommandResultWrapper
        {
            public SendCommandResult? SendCommandResult { get; set; }
        }

        /// <summary>Deserializes the SendCommand-routed "Wrapped" response and returns just the
        /// inner result -- for the handful of callers that need the isolated Text value rather
        /// than raw-string matching (SendGetPGMCommand, battery status, installer commands,
        /// SendCommandToIdentifier, time sync -- matching the original's explicit
        /// JsonConvert.DeserializeObject&lt;SendCommandResultWraper&gt; call sites).</summary>
        private async Task<SendCommandResult?> ExecuteWrappedAsync(object body)
        {
            var content = await ExecuteAsync("SendCommand", body);
            try
            {
                return JsonSerializer.Deserialize<SendCommandResultWrapper>(content)?.SendCommandResult;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private string GetStatus(string content)
        {
            var res = "";
            if (content.Contains("NOT-READY"))
                res = "NOT-READY";
            else if (content.Contains("READY"))
                res = "READY";
            else if (content.Contains("ARM"))
            {
                res = "ARM";
                if (content.Contains("STAY"))
                    res += ",STAY";
                if (content.Contains("NIGHT"))
                    res += ",NIGHT";
                if (content.Contains("AWAY"))
                    res += ",AWAY";
                if (content.Contains("NDLY"))
                    res += ",NDLY";
            }
            if (!string.IsNullOrEmpty(res))
            {
                if (content.Contains("BELL"))
                    res += ",BELL";
                if (content.Contains("BYPASS"))
                    res += ",BYPASS";
                if (content.Contains("MEMO"))
                    res += ",MEMO";
            }
            else if (content.Contains("ERROR"))
                res = "ERROR";
            else if (content.Contains("DISCONECTED"))
                return "";
            else if (content.Contains("OK"))
                return "OK";
            return res;
        }

        private string GetVersion(string content)
        {
            var res = content.Substring(content.IndexOf("VER:") + 4);
            res = res.Remove(res.IndexOf('"')).Replace(",", ".");
            return res;
        }

        private string GetByPassResponse(string content)
        {
            if (content.Contains("DISCONECTED"))
                return "DST";
            if (content.Contains("BYP:"))
            {
                var subs = content.Substring(content.IndexOf("BYP:") + 4);
                subs = subs.Remove(subs.IndexOf('"'));
                return subs;
            }
            return GetStatus(content);
        }

        private string GetZonesResponse(string content)
        {
            if (content.Contains("DISCONECTED"))
                return "DST";
            if (content.Contains("STSZ:"))
            {
                var subs = content.Substring(content.IndexOf("STSZ:") + 5);
                subs = subs.Remove(subs.IndexOf('"'));
                var zones = subs.Split(',');
                var res = "";
                foreach (var item in zones)
                {
                    res += item.Length < 2 ? "0" + item + "," : item + ",";
                }
                res = res.Remove(res.LastIndexOf(','));
                return res;
            }
            return GetStatus(content);
        }

        private string GetZonesListResponse(string content, string command)
        {
            if (content.Contains("DISCONECTED"))
                return "DST";
            var header = command + ":";
            if (content.Contains(header))
            {
                var subs = content.Substring(content.IndexOf(header) + header.Length);
                subs = subs.Remove(subs.IndexOf('"'));
                var zones = subs.Split(',');
                var res = "";
                foreach (var item in zones)
                {
                    res += item.Length < 2 ? "0" + item + "," : item + ",";
                }
                res = res.Remove(res.LastIndexOf(','));
                return res;
            }
            return GetStatus(content);
        }
    }
}
