using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TausendRelay.Business;
using TausendRelay.Cache;
using TausendRelay.Commands;
using TausendRelay.Encryption;
using TausendRelay.Enums;

namespace TausendRelay.Relay
{
    /// <summary>
    /// Port of backend/Tausend.UDPListener/Listener/Listener.cs -- the actual panel-facing UDP
    /// socket, CGM1 framing, and command dispatch. Runs as a hosted background service; also
    /// registered for direct injection so PrivateService's minimal API endpoints can call
    /// SendCommandAsync/SendPgmCommandAsync/SendBypCommandAsync directly.
    ///
    /// Two correctness fixes from the original, beyond DeviceCache's locking (see its own doc
    /// comment):
    /// - The per-device pending-response table (StatusRes in the original) was a plain
    ///   Dictionary read/written from both the receive loop and every concurrent SendCommand call
    ///   with no lock at all -- now a ConcurrentDictionary.
    /// - SendCommand's fallback value was "DISCONNECTED", but both backends' CommandBusiness.GetStatus
    ///   check for "DISCONECTED" (a pre-existing typo in the *original* Tausend.Core.Business, not
    ///   introduced during this port -- confirmed present in both backend/Tausend.Core and
    ///   backend-core's ported copy). The relay's spelling never matched what either backend was
    ///   looking for, so a genuinely offline device has likely never reported as such correctly.
    ///   This port uses the backends' spelling so the two sides finally agree.
    /// </summary>
    public class UdpRelayService : BackgroundService
    {
        private readonly DeviceCache _deviceCache;
        private readonly BackendClient _backendClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UdpRelayService> _logger;
        private readonly Cgm1 _cgm1 = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<CommandResponseType, string>> _statusResponses = new();

        private Socket? _socket;

        public UdpRelayService(DeviceCache deviceCache, BackendClient backendClient, IConfiguration configuration, ILogger<UdpRelayService> logger)
        {
            _deviceCache = deviceCache;
            _backendClient = backendClient;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bindAddress = IPAddress.Parse(_configuration["Udp:BindAddress"] ?? "0.0.0.0");
            var port = int.Parse(_configuration["Udp:Port"] ?? "10000");
            var localEndPoint = new IPEndPoint(bindAddress, port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(localEndPoint);
            _logger.LogInformation("UDP relay listening on {EndPoint}", localEndPoint);

            var buffer = new byte[512];
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var receiveResult = await _socket.ReceiveFromAsync(
                        buffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), stoppingToken);
                    var remote = receiveResult.RemoteEndPoint;
                    var data = Encoding.ASCII.GetString(buffer, 0, receiveResult.ReceivedBytes);
                    if (data.Contains('>'))
                        data = data[..(data.IndexOf('>') + 1)];

                    if (!DecodeMessage(data, out var destine, out var source, out var msgType, out var payload))
                        continue;
                    if (msgType == MessageType.Data)
                        payload = Decrypt(source, payload);

                    await ProcessMessageAsync(source, destine, msgType, payload, remote);
                    CheckResponseType(source, payload);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error handling incoming UDP packet");
                }
            }
        }

        private string Decrypt(int source, string payload)
        {
            var device = _deviceCache.GetById(source);
            return device is { PublicKey: > 0 } ? _cgm1.DecryptToString(payload, device.PublicKey) : payload;
        }

        private string Encrypt(int destine, string payload)
        {
            var device = _deviceCache.GetById(destine);
            return device is { PublicKey: > 0 } ? _cgm1.Encrypt(payload, device.PublicKey) : payload;
        }

        private async Task ProcessMessageAsync(int source, int destine, MessageType msgType, string payload, EndPoint remote)
        {
            switch (msgType)
            {
                case MessageType.Id:
                    await ProcessIdMessageAsync(payload, remote);
                    break;
                case MessageType.Idq:
                    ProcessIdqMessage(payload, remote, source);
                    break;
                case MessageType.Event:
                    await ProcessEventMessageAsync(source, remote, payload);
                    break;
                case MessageType.Unknown:
                case MessageType.Data:
                case MessageType.Con:
                default:
                    RelayMessage(source, destine, msgType, payload, remote);
                    break;
            }
        }

        private void CheckResponseType(int source, string payload)
        {
            var responses = _statusResponses.GetOrAdd(source, _ => new ConcurrentDictionary<CommandResponseType, string>());

            if (payload.Contains("STSZ")) responses[CommandResponseType.ZoneStatus] = payload;
            // Real panels sometimes send the bare token ("STSA"/"DAR"/"ARM") with no trailing
            // ":<data>" -- stripping a fixed-length prefix off a too-short payload used to throw
            // and drop the whole packet (caught, logged, but the status update was lost). Fall
            // back to the raw payload instead of crashing when it's shorter than expected.
            else if (payload.Contains("STSA")) responses[CommandResponseType.GeneralStatus] = payload.Length > 5 ? payload[5..] : payload;
            else if (payload.Contains("STSF")) responses[CommandResponseType.FailStatus] = payload;
            else if (payload.Contains("STSM")) responses[CommandResponseType.Memory] = payload;
            else if (payload.Contains("DAR")) responses[CommandResponseType.Disarm] = payload.Length > 4 ? payload[4..] : payload;
            else if (payload.Contains("ARM")) responses[CommandResponseType.Arm] = payload.Length > 4 ? payload[4..] : payload;
            else if (payload.Contains("ERROR")) responses[CommandResponseType.Generic] = payload;
            else if (payload.Contains("OK")) responses[CommandResponseType.Generic] = payload;
            else if (payload.Contains("BYP")) responses[CommandResponseType.Bypass] = payload;
            else if (payload.Contains("VER")) responses[CommandResponseType.Version] = payload;
            else if (payload.Contains("PGM")) responses[CommandResponseType.ProgramControl] = payload;
            else if (payload.Contains("RTC")) responses[CommandResponseType.Clock] = payload;
            else if (payload.Contains("PRG")) responses[CommandResponseType.Program] = payload;
            else if (payload.Contains("STSB")) responses[CommandResponseType.Battery] = payload;
            else responses[CommandResponseType.Installer] = payload;
        }

        private async Task ProcessEventMessageAsync(int source, EndPoint remote, string payload)
        {
            // (ssss,0000)05<qqq:dd-mm-yy hh:mm:ss TEEE-PP-ppp> -- Contact-ID event report. See the
            // original Listener.cs for the full field layout comment; preserved verbatim here.
            _logger.LogInformation("EVENT from {Source} => {Payload}", source, payload);
            try
            {
                var central = _deviceCache.GetById(source);
                if (central is null)
                    return;

                var sequenceStr = payload.Substring(0, 3);
                var dateTimeStr = payload.Substring(4, 17);
                var eventTypeStr = payload.Substring(22, 1);
                var cidTypeStr = payload.Substring(23, 3);
                var partStr = payload.Substring(27, 2);
                var parameterStr = payload.Substring(30, 3);
                var fullCid = payload.Substring(22, 4);

                var ackCmd = MakeRelayMessage(0, source, MessageType.EventOk, sequenceStr);
                SendMessage(ackCmd, remote);

                var sequence = int.Parse(sequenceStr);
                if (fullCid == "E000" && parameterStr == "001")
                    await _backendClient.DisassociateDeviceAsync(central.Identifier);

                if (_deviceCache.IsNewMessage(source, sequence))
                {
                    var cid = int.Parse(cidTypeStr);
                    var part = int.Parse(partStr);
                    var parameter = int.Parse(parameterStr);
                    await _backendClient.NotifyEventAsync(central.Identifier, parameter, dateTimeStr, eventTypeStr, cid, part, sequence);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Event not forwarded");
            }
        }

        /// <summary>
        /// destine != 0 means "forward this to another device" (device-to-device passthrough);
        /// destine == 0 means "this message was addressed to the server", which for the
        /// UNKNOWN/DATA/CON types handled here just means using the sender's address to refresh
        /// that device's known IP:port (handles NAT/dynamic-IP churn).
        /// </summary>
        private void RelayMessage(int source, int destine, MessageType msgType, string payload, EndPoint remote)
        {
            if (destine != 0)
            {
                var device = _deviceCache.GetById(destine);
                if (device is not null)
                {
                    var cmd = MakeRelayMessage(source, destine, msgType, payload);
                    SendMessage(cmd, new IPEndPoint(device.Ip, device.Port));
                }
            }
            else
            {
                GetIpAndPort(remote, out var ip, out var port);
                _deviceCache.Refresh(source, ip, port);
            }
        }

        private string MakeRelayMessage(int source, int destine, MessageType code, string payload)
        {
            if (code == MessageType.Data)
                payload = Encrypt(destine, payload);
            return '(' + source.ToString("X4") + "," + destine.ToString("X4") + ")" + ((int)code).ToString("X2") + "<" + payload + ">";
        }

        private static bool IsHex(string s) => s.All(Uri.IsHexDigit);

        private static bool DecodeMessage(string message, out int destine, out int source, out MessageType messageType, out string payload)
        {
            destine = 0;
            source = 0;
            messageType = MessageType.Unknown;
            payload = "";
            if (message.Length < 13)
                return false;
            if (message[0] != '(' || message[5] != ',' || message[10] != ')')
                return false;
            if (!IsHex(message.Substring(1, 4) + message.Substring(6, 4) + message.Substring(11, 2)))
                return false;

            source = Convert.ToInt32(message.Substring(1, 4), 16);
            destine = Convert.ToInt32(message.Substring(6, 4), 16);
            messageType = (MessageType)Convert.ToInt32(message.Substring(11, 2), 16);

            if (message.Length < 14)
                return true;
            if (message.Length < 15)
                return false;
            if (message[13] != '<' || message[^1] != '>')
                return false;

            payload = message.Substring(14, message.Length - 15);
            return true;
        }

        private void SendMessage(string message, EndPoint remoteEndpoint)
        {
            var bytes = Encoding.ASCII.GetBytes(message);
            _socket!.SendTo(bytes, bytes.Length, SocketFlags.None, remoteEndpoint);
        }

        private static void GetIpAndPort(EndPoint endpoint, out IPAddress ip, out int port)
        {
            ip = ((IPEndPoint)endpoint).Address;
            port = ((IPEndPoint)endpoint).Port;
        }

        // Panels re-send this registration message roughly every 10s as a heartbeat, but nothing
        // here ever told the backend a panel was connected at all -- the whole "is this panel
        // online" concept lived only in this relay's own in-memory DeviceCache, so the backend's
        // Devices.IsOnline column (what the app actually displays) never reflected real
        // connectivity. Throttled well under GetDeviceByIdentifier.sql's 1-hour staleness window
        // so a genuinely-connected panel never gets auto-marked offline, without turning every
        // single heartbeat into an outbound HTTP call.
        private static readonly TimeSpan MinBackendSyncInterval = TimeSpan.FromMinutes(2);

        private async Task ProcessIdMessageAsync(string payload, EndPoint remote)
        {
            var identifier = payload;
            GetIpAndPort(remote, out var ip, out var port);
            var device = _deviceCache.GetByIdentifier(identifier);
            if (device is null)
                device = await _deviceCache.AddCentralDeviceAsync(identifier, ip, port);
            else
                _deviceCache.Refresh(device.Id, ip, port);

            if (device is null)
                return; // cache full (see DeviceCache.MaxDevices)

            var cmd = MakeRelayMessage(0, device.Id, MessageType.IdOk, identifier);
            _logger.LogInformation("Device registered: {Identifier} ({RelayId:X4}) at {Ip}:{Port}", identifier, device.Id, ip, device.Port);
            SendMessage(cmd, remote);

            var backendDeviceId = _deviceCache.TryClaimBackendSync(device.Id, MinBackendSyncInterval);
            if (backendDeviceId > 0)
                await _backendClient.UpdateDeviceLastConnectionAsync(backendDeviceId);
        }

        private void ProcessIdqMessage(string payload, EndPoint remote, int source)
        {
            var identifier = payload;
            var device = _deviceCache.GetByIdentifier(identifier);
            var response = device is not null ? device.Id.ToString("X4") : identifier;
            var cmd = MakeRelayMessage(0, source, MessageType.IdqOk, response);
            SendMessage(cmd, remote);
        }

        public async Task<string> SendCommandAsync(string command, string identifier, string pin)
        {
            try
            {
                var device = _deviceCache.GetByIdentifier(identifier);
                if (device is null)
                    return "DISCONECTED"; // matches CommandBusiness.GetStatus's spelling on both backends

                var responses = _statusResponses.GetOrAdd(device.Id, _ => new ConcurrentDictionary<CommandResponseType, string>());

                string txt;
                var responseType = CommandResponseType.None;
                if (command.Contains("INST-"))
                {
                    txt = command[5..];
                    responseType = CommandResponseType.Installer;
                    responses.Clear();
                }
                else if (command.Contains(CommandCodes.Arm))
                {
                    var armMode = command == "ARMA" ? ArmMode.Away : command == "ARMN" ? ArmMode.Night : ArmMode.Stay;
                    txt = new ArmCommand(armMode, pin).GetIpCommand();
                    responseType = CommandResponseType.Arm;
                }
                else if (command == CommandCodes.Disarm)
                {
                    txt = new DisarmCommand(pin).GetIpCommand();
                    responseType = CommandResponseType.Disarm;
                }
                else if (command == "STSZ")
                {
                    txt = new StatusCommand(StatusOptions.Zone).GetIpCommand();
                    responseType = CommandResponseType.ZoneStatus;
                }
                else if (command == "STSM")
                {
                    txt = new StatusCommand(StatusOptions.Memory).GetIpCommand();
                    responseType = CommandResponseType.Memory;
                }
                else if (command == "STSF")
                {
                    txt = new StatusCommand(StatusOptions.Fail).GetIpCommand();
                    responseType = CommandResponseType.FailStatus;
                }
                else if (command == CommandCodes.Status)
                {
                    txt = new StatusCommand().GetIpCommand();
                    responseType = CommandResponseType.GeneralStatus;
                }
                else if (command.Contains("FUN"))
                {
                    txt = command;
                    responseType = CommandResponseType.Generic;
                }
                else if (command.Contains("BYP"))
                {
                    txt = command;
                    responseType = CommandResponseType.Bypass;
                }
                else if (command.Contains("PGM"))
                {
                    txt = command;
                    responseType = CommandResponseType.ProgramControl;
                }
                else if (command.Contains("RTC"))
                {
                    txt = command;
                    responseType = CommandResponseType.Clock;
                }
                else if (command.Contains("PRG"))
                {
                    txt = command;
                    responseType = CommandResponseType.Program;
                }
                else if (command.Contains("STSB"))
                {
                    txt = command;
                    responseType = CommandResponseType.Battery;
                }
                else if (command == CommandCodes.Version)
                {
                    txt = new VersionCommand().GetIpCommand();
                    responseType = CommandResponseType.Version;
                }
                else
                {
                    txt = command;
                }

                var cmd = MakeRelayMessage(0, device.Id, MessageType.Data, txt);
                var endpoint = new IPEndPoint(device.Ip, device.Port);

                if (responseType != CommandResponseType.None)
                    responses.TryRemove(responseType, out _);

                var res = "OK";
                await _sendLock.WaitAsync();
                try
                {
                    SendMessage(cmd, endpoint);
                    if (responseType != CommandResponseType.None)
                    {
                        res = "DISCONECTED";
                        var i = 0;
                        while (!ProcessResponseType(responses, responseType) && i < 10)
                        {
                            await Task.Delay(200);
                            i++;
                        }
                        if (responses.TryRemove(responseType, out var direct))
                        {
                            res = direct;
                        }
                        else if (responses.TryGetValue(CommandResponseType.Generic, out var generic) && generic == "ERROR")
                        {
                            responses.TryRemove(CommandResponseType.Generic, out _);
                            res = generic;
                        }
                        else if (responseType == CommandResponseType.Installer && !responses.IsEmpty)
                        {
                            var key = responses.Keys.First();
                            responses.TryRemove(key, out var installerRes);
                            res = installerRes ?? res;
                        }
                    }
                }
                finally
                {
                    _sendLock.Release();
                }
                return res;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SendCommand failed for {Identifier}", identifier);
                return "DISCONECTED";
            }
        }

        private static bool ProcessResponseType(ConcurrentDictionary<CommandResponseType, string> responses, CommandResponseType type)
        {
            if (type == CommandResponseType.Installer && !responses.IsEmpty)
                return true;
            if (responses.ContainsKey(type))
                return true;
            return responses.TryGetValue(CommandResponseType.Generic, out var generic) && generic == "ERROR";
        }

        public Task SendPgmCommandAsync(string identifier, int zone, bool state)
        {
            try
            {
                var device = _deviceCache.GetByIdentifier(identifier);
                if (device is null)
                    return Task.CompletedTask;
                var txt = new ProgramControlCommand(zone, state).GetIpCommand();
                var cmd = MakeRelayMessage(0, device.Id, MessageType.Data, txt);
                SendMessage(cmd, new IPEndPoint(device.Ip, device.Port));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SendPGMCommand failed for {Identifier}", identifier);
            }
            return Task.CompletedTask;
        }

        public Task SendBypCommandAsync(string identifier, List<int> zones)
        {
            try
            {
                var device = _deviceCache.GetByIdentifier(identifier);
                if (device is null)
                    return Task.CompletedTask;
                var txt = new ExclusionCommand(zones).GetIpCommand();
                var cmd = MakeRelayMessage(0, device.Id, MessageType.Data, txt);
                SendMessage(cmd, new IPEndPoint(device.Ip, device.Port));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SendBYPCommand failed for {Identifier}", identifier);
            }
            return Task.CompletedTask;
        }
    }
}
