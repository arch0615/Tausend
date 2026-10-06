using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Tausend.RelayServer.Settings;
using Tausend.UDPListener.Enums;
using Tausend.UDPListener.Cache;
using Tausend.Backend.Enums;
using Tausend.RelayServer.Commands;
using Tausend.RelayServer.Enums;
using System.Threading;
using Tausend.Core.Entities.Requests;
using System.Globalization;
using Tausend.RelayServer.Business;
using Tausend.RelayServer.Entities.Enums;
using GAMLib;
using System.Security.Cryptography;
using System.Web.UI.WebControls.WebParts;

namespace Tausend.UDPListener.Listener
{
    public class UDPListener
    {
        static Socket listener;
        static Dictionary<int, Dictionary<CommandResponseType, string>> StatusRes = new Dictionary<int, Dictionary<CommandResponseType, string>>();
        private static readonly object locker = new object();
        static CGM1 encryptBz;

        public static void StartServer()
        {
            CacheManager.LoadCache();
            encryptBz = new CGM1();
            var ns = SettingsManager.Instance.GetNetworkSettings();
            _ = ns.IP;
            var localPort = ns.Port;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, localPort);

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (int)1);
                listener.Bind(localEndPoint);

                string data = null;
                byte[] bytes = null;
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                while (true)
                {
                    try
                    {
                        bytes = new byte[512];

                        EndPoint remote = sender;
                        var bytesRec = listener.ReceiveFrom(bytes, ref remote);
                        data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                        Console.WriteLine("Comando recibido: {0}", data);
                        if (data.Contains('>'))
                            data = data.Substring(0, data.IndexOf('>') + 1);
                        DecodeMessage(data, out int destine, out int source, out MessageType msgType, out string payload);
                        ProcessMessage(source, destine, msgType, payload, remote);
                        CheckResponseType(source, payload);
                    }
                    catch (Exception e)
                    {
                        HandleException(e);
                    }
                }
            }
            catch (Exception e)
            {
                HandleException(e);
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }

        private static string Decrypt(int source, string payload)
        {
            string res = payload;
            var dev = CacheManager.GetCentralDeviceByID(source);
                if (dev?.PublicKey > 0)
                    res = encryptBz.decriptStr(payload, dev.PublicKey);
            return res;
        }

        private static string Encrypt(int destine, string payload)
        {
            string res = payload;
            var dev = CacheManager.GetCentralDeviceByID(destine);
            if (dev?.PublicKey > 0)
                res = encryptBz.encript(payload, dev.PublicKey);
            return res;
        }

        private static void ProcessMessage(int source, int destine, MessageType msgType, string payload, EndPoint remote)
        {
            switch (msgType)
            {
                case MessageType.UNKNOWN:
                    RelayMessage(source, destine, msgType, payload, remote);
                    break;
                case MessageType.ID:
                    ProcessIdMessage(payload, remote, source);
                    break;
                case MessageType.IDQ:
                    ProcessIDQMessage(payload, remote, source);
                    break;
                case MessageType.DATA:
                    RelayMessage(source, destine, msgType, payload, remote);
                    break;
                case MessageType.CON:
                    RelayMessage(source, destine, msgType, payload, remote);
                    break;
                case MessageType.EVENT:
                    ProcessEventMessage(source, remote, payload);
                    break;
                default:
                    RelayMessage(source, destine, msgType, payload, remote);
                    break;
            }
        }

        private static void CheckResponseType(int source, string payload)
        {
            if (!StatusRes.ContainsKey(source))
                StatusRes.Add(source, new Dictionary<CommandResponseType, string>());
            if (payload.Contains("STSZ"))
            {
                AddStatusResponse(source, CommandResponseType.ZoneStatus, payload);
            }
            else if (payload.Contains("STSA"))
            {
                AddStatusResponse(source, CommandResponseType.GeneralStatus, payload.Substring(5));
            }
            else if (payload.Contains("STSF"))
            {
                AddStatusResponse(source, CommandResponseType.FailStatus, payload);
            }
            else if (payload.Contains("STSM"))
            {
                AddStatusResponse(source, CommandResponseType.Memory, payload);
            }
            else if (payload.Contains("DAR"))
            {
                AddStatusResponse(source, CommandResponseType.Disarm, payload.Substring(4));
            }
            else if (payload.Contains("ARM"))
            {
                AddStatusResponse(source, CommandResponseType.Arm, payload.Substring(4));
            }
            else if (payload.Contains("ERROR"))
            {
                AddStatusResponse(source, CommandResponseType.Generic, payload);
            }
            else if (payload.Contains("OK"))
            {
                AddStatusResponse(source, CommandResponseType.Generic, payload);
            }
            else if (payload.Contains("BYP"))
            {
                AddStatusResponse(source, CommandResponseType.Bypass, payload);
            }
            else if (payload.Contains("VER"))
            {
                AddStatusResponse(source, CommandResponseType.Version, payload);
            }
            else if (payload.Contains("PGM"))
            {
                AddStatusResponse(source, CommandResponseType.ProgramControl, payload);
            }
            else if (payload.Contains("RTC"))
            {
                AddStatusResponse(source, CommandResponseType.Clock, payload);
            }
            else if (payload.Contains("PRG"))
            {
                AddStatusResponse(source, CommandResponseType.Program, payload);
            }
            else if (payload.Contains("STSB"))
            {
                AddStatusResponse(source, CommandResponseType.Battery, payload);
            }
            else
            {
                AddStatusResponse(source, CommandResponseType.Installer, payload);
            }
        }

        private static void AddStatusResponse(int source, CommandResponseType type, string payload)
        {
            if (StatusRes[source].ContainsKey(type))
                StatusRes[source].Remove(type);
            StatusRes[source].Add(type, payload);
        }

        private static void ProcessEventMessage(int source, EndPoint remote, string payload)
        {
            /*
             * (ssss,0000)05<qqq:dd-mm-yy hh:mm:ss TEEE-PP-ppp>
             
                Este mensaje es enviado por el dispositivo para reportar un evento tipo Contact-ID. El destino es
                siempre el servidor, o sea 0000. El payload tiene exactamente el formato indicado y contiene:

                     Numero de secuencia qqq (de 0 a 255)
                     fecha y hora del evento dd-mm-yy hh:mm:ss
                     Tipo de evento T, que puede ser: E=evento, R= reposición
                     Código CID del evento EEE (ver tabla de códigos CID)
                     Partición PP (1 a 4, 0 es evento de sistema no referido a ninguna partición en especial)
                     Parámetro ppp: identifica zona, usuario o teclado (según el evento)
             */
            Console.WriteLine("EVENT from " + source + "=> " + payload);
            try
            {
                var central = CacheManager.GetCentralDeviceByID(source);
                if (central != null)
                {
                    var sequenceStr = payload.Substring(0, 3);
                    var datetimeStr = payload.Substring(4, 17);
                    var eventTypeStr = payload.Substring(22, 1);
                    var cidTypeStr = payload.Substring(23, 3);
                    var partStr = payload.Substring(27, 2);
                    var parameterStr = payload.Substring(30, 3);
                    var fullCid = payload.Substring(22, 4);

                    var cmd = MakeRelayMessage(0, source, MessageType.EVENTOK, sequenceStr);
                    SendMessage(cmd, remote);

                    var sequence = int.Parse(sequenceStr);
                    if(fullCid == "E000" && parameterStr == "001")
                    {
                        var bz = new ServiceBusiness();
                        bz.DisassociateDevice(central.Identifier);
                    }
                    if (CacheManager.IsNewMessage(source, sequence))
                    {
                        //var datetime = DateTime.ParseExact(datetimeStr, "dd-MM-yy HH:mm:ss", CultureInfo.InvariantCulture);
                        var eventType = eventTypeStr;
                        var cid = int.Parse(cidTypeStr);
                        var part = int.Parse(partStr);
                        var parameter = int.Parse(parameterStr);
                        var bz = new ServiceBusiness();
                        bz.NotifyEvent(central.Identifier, parameter, datetimeStr, eventType, cid, part, sequence);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Evento no enviado: " + e.Message);
            }
        }

        private static void RelayMessage(int source, int destine, MessageType msgType, string payload, EndPoint remote)
        {
            if (destine != 0)
            {
                var device = CacheManager.GetCentralDeviceByID(destine);
                if (device != null)
                {
                    var cmd = MakeRelayMessage(source, destine, msgType, payload);
                    SendMessage(cmd, new IPEndPoint(device.IP, device.Port));
                }
            }
            else
            {
                GetIPAddressAndPortFromEndpoint(remote, out IPAddress ip, out int port);
                CacheManager.RefreshCentralDeviceData(source, ip, port);
            }
        }

        static string MakeRelayMessage(int source, int destine, MessageType code, string payload)
        {
            if(code == MessageType.DATA)
                payload = Encrypt(destine, payload);
            return '(' + source.ToString("X4") + "," + destine.ToString("X4") + ")" + ((int)code).ToString("X2") + "<" + payload + ">";
        }

        // Teste si el texto es un numero hexadecimal
        static bool IsHex(string s)
        {
            return s.All("0123456789abcdefABCDEF".Contains);
        }

        // Decodigica un mensaje y extrae los campos
        static bool DecodeMessage(string message, out int destine, out int source, out MessageType messageType, out string payload)
        {
            destine = 0;
            source = 0;
            messageType = MessageType.UNKNOWN;
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

            // Mensaje sin payload
            if (message.Length < 14)
            {
                payload = "";
                return true;
            }

            if (message.Length < 15)
                return false;

            if (message[13] != '<' || message[message.Length - 1] != '>')
                return false;

            payload = message.Substring(14, message.Length - 15);
            if (messageType == MessageType.DATA)
                payload = Decrypt(source, payload);
            return true;
        }

        private static void SendMessage(string message, EndPoint remoteEndpoint)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            Console.WriteLine("Sending: " + message);
            listener.SendTo(bytes, bytes.Length, SocketFlags.None, remoteEndpoint);
        }

        private static void GetIPAddressAndPortFromEndpoint(EndPoint endpoint, out IPAddress remoteIP, out int remotePort)
        {
            remoteIP = ((IPEndPoint)endpoint).Address;
            remotePort = ((IPEndPoint)endpoint).Port;
        }

        private static void ProcessIdMessage(string payload, EndPoint remote, long source = 0)
        {
            string cmd;
            string identifier = payload;
            GetIPAddressAndPortFromEndpoint(remote, out IPAddress ip, out int port);
            var device = CacheManager.GetCentralDeviceByIdentifier(identifier);
            if (device == null)
            {
                device = CacheManager.AddCentralDevice(identifier, ip, port, source);
            }
            else
            {
                CacheManager.RefreshCentralDeviceData(device.ID, ip, port);
            }
            cmd = MakeRelayMessage(0, device.ID, MessageType.IDOK, identifier);
            Console.WriteLine("Device: " + ip.ToString() + ":" + device.Port);
            SendMessage(cmd, remote);
        }

        private static void ProcessIDQMessage(string payload, EndPoint remote, int source)
        {
            string cmd;
            string identifier = payload;
            GetIPAddressAndPortFromEndpoint(remote, out IPAddress ip, out int port);
            var device = CacheManager.GetCentralDeviceByIdentifier(identifier);
            string response;
            if (device != null)
            {
                response = device.ID.ToString("X4");
            }
            else
            {
                response = identifier;
            }
            cmd = MakeRelayMessage(0, source, MessageType.IDQOK, response);
            Console.WriteLine("IDQREQ from: " + ip.ToString() + ":" + port);
            SendMessage(cmd, remote);
        }

        public static string SendCommand(string command, string identifier, string pin)
        {
            try
            {
                var res = "DISCONNECTED";
                var device = CacheManager.GetCentralDeviceByIdentifier(identifier);
                if (device == null)
                    return res;
                var endpoint = new IPEndPoint(device.IP, device.Port);
                string txt;
                var responseType = CommandResponseType.None;
                if (command.Contains("INST-"))
                {
                    txt = command.Substring(5);
                    responseType = CommandResponseType.Installer;
                    StatusRes[device.ID].Clear();
                }
                else if (command.Contains(COMMANDS.ARM))
                {
                    var armMode = command.Equals("ARMA") ? ArmMode.AWAY : command.Equals("ARMN") ? ArmMode.NIGHT : ArmMode.STAY;
                    var arm = new ArmCommand(armMode, pin);
                    txt = arm.GetIpCommand();
                    responseType = CommandResponseType.Arm;
                }
                else if (command.Equals(COMMANDS.DISARM))
                {
                    var disarm = new DisarmCommand(pin);
                    txt = disarm.GetIpCommand();
                    responseType = CommandResponseType.Disarm;
                }
                else if (command.Equals("STSZ"))
                {
                    var status = new StatusCommand(StatusOptions.ZONE);
                    txt = status.GetIpCommand();
                    responseType = CommandResponseType.ZoneStatus;
                }
                else if (command.Equals("STSM"))
                {
                    var status = new StatusCommand(StatusOptions.MEMORY);
                    txt = status.GetIpCommand();
                    responseType = CommandResponseType.Memory;
                }
                else if (command.Equals("STSF"))
                {
                    var status = new StatusCommand(StatusOptions.FAIL);
                    txt = status.GetIpCommand();
                    responseType = CommandResponseType.FailStatus;
                }
                else if (command.Equals(COMMANDS.STATUS))
                {
                    var status = new StatusCommand();
                    txt = status.GetIpCommand();
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
                else if (command.Equals(COMMANDS.VERSION))
                {
                    var version = new VersionCommand();
                    txt = version.GetIpCommand();
                    responseType = CommandResponseType.Version;
                }
                else
                {
                    txt = command;
                }
                var cmd = MakeRelayMessage(0, device.ID, MessageType.DATA, txt);
                Console.WriteLine("Device " + identifier + ": " + device.IP + ":" + device.Port + " sending " + cmd);
                if (responseType != CommandResponseType.None)
                {
                    if(StatusRes.ContainsKey(device.ID) && StatusRes[device.ID].ContainsKey(responseType))
                        StatusRes[device.ID].Remove(responseType);
                }
                lock (locker)
                {
                    SendMessage(cmd, endpoint);
                    if (responseType != CommandResponseType.None)
                    {
                        int i = 0;
                        while (!ProcessResponseType(device.ID, responseType) && i < 10)
                        {
                            Thread.Sleep(200);
                            i++;
                        }
                        if (StatusRes.ContainsKey(device.ID))
                        {
                            if (StatusRes[device.ID].ContainsKey(responseType))
                            {
                                res = StatusRes[device.ID][responseType];
                                StatusRes[device.ID].Remove(responseType);
                            }
                            else if (StatusRes[device.ID].ContainsKey(CommandResponseType.Generic) && StatusRes[device.ID][CommandResponseType.Generic].Equals("ERROR"))
                            {
                                res = StatusRes[device.ID][CommandResponseType.Generic];
                                StatusRes[device.ID].Remove(CommandResponseType.Generic);
                            }
                            else if (responseType == CommandResponseType.Installer)
                            {
                                var key = StatusRes[device.ID].Keys.First();
                                res = StatusRes[device.ID][key];
                                StatusRes[device.ID].Remove(key);
                            }
                        }
                        Console.WriteLine("Estado de respuesta: " + res);
                    }
                    else
                    {
                        res = "OK";
                    }
                }
                return res;
            }
            catch (Exception e)
            {
                HandleException(e);
                return "DISCONNECTED";
            }
        }

        private static bool ProcessResponseType(int source, CommandResponseType type)
        {
            if (type == CommandResponseType.Installer && StatusRes[source].Count > 0)
                return true;
            if (!StatusRes.ContainsKey(source))
                return false;
            if (StatusRes[source].ContainsKey(type))
                return true;
            if (StatusRes[source].ContainsKey(CommandResponseType.Generic))
                return StatusRes[source][CommandResponseType.Generic].Equals("ERROR");
            return false;
        }

        private static void HandleException(Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

        public static string SendPGMCommand(string identifier, int zone, bool state)
        {
            try
            {

                var device = CacheManager.GetCentralDeviceByIdentifier(identifier);
                var endpoint = new IPEndPoint(device.IP, device.Port);
                var pgm = new ProgramControlCommand(zone, state);
                var txt = pgm.GetIpCommand();
                var cmd = MakeRelayMessage(0, device.ID, MessageType.DATA, txt);
                Console.WriteLine("Device " + identifier + ": " + device.IP + ":" + device.Port + " sending " + cmd);
                SendMessage(cmd, endpoint);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
            return "";
        }

        public static string SendBYPCommand(string identifier, List<int> zones)
        {
            try
            {
                var device = CacheManager.GetCentralDeviceByIdentifier(identifier);
                var endpoint = new IPEndPoint(device.IP, device.Port);
                var pgm = new ExclusionCommand(zones);
                var txt = pgm.GetIpCommand();
                var cmd = MakeRelayMessage(0, device.ID, MessageType.DATA, txt);
                Console.WriteLine("Device " + identifier + ": " + device.IP + ":" + device.Port + " sending " + cmd);
                SendMessage(cmd, endpoint);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
            return "";
        }

    }
}
