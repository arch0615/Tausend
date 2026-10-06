using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Tausend.Backend.Enums;
using Tausend.Core.Business;
using Tausend.Core.Entities.Responses.CommandService;
using Tausend.Core.Models;
using Tausend.Core.Responses;
using Tausend.Core.Responses.PrivateService;
using Tausend.Core.Security;

namespace Tausend.Backend.Business
{
    public class CommandBusiness
    {
        public string ArmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.ARM + "A", commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public string DayArmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.ARM + "S", commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetStatus(res);
            }
            catch (Exception e) { return e.Message; }
        }

        public string DisarmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.DISARM, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetStatus(res.Content);
            }
            catch (Exception e)
            { return e.Message; }
        }

        public string NightArmAlarm(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.ARM + "N", commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetStatus(res.Content);
            }
            catch (Exception e)
            { return e.Message; }
        }

        public string Exclusion(ExclusionRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "BYPCommand";
                var request = CreateRequest(commandRequest, COMMANDS.BYPASS);
                var res = Execute(serviceUrl, MethodName, request);
                return GetStatus(res);
            }
            catch (Exception e)
            { return e.Message; }
        }

        public string PGMCommand(PGMRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "PGMCommand";
                var request = CreateRequest(commandRequest, COMMANDS.PROGRAM_CONTROL);
                var res = Execute(serviceUrl, MethodName, request);
                //var sendCommandResult = JsonConvert.DeserializeObject<SendCommandResultWraper>(res.Content);
                return "OK";
            }
            catch (Exception e)
            { return e.Message; }
        }

        public string GetFirmwareVersion(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.VERSION, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetVersion(res);
            }
            catch (Exception e)
            { return e.Message; }
        }

        public string GetGeneralStatus(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetStatus(res.Content);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string GetFailStatus(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.FAIL_STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                var content = res.Content;
                if (content.Contains("STSF:"))
                {
                    var subs = content.Substring(content.IndexOf("STSF:") + 5);
                    subs = subs.Remove(subs.IndexOf("\""));
                    return subs;
                }
                else
                    return GetStatus(content);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string GetZonesExclusion(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.BYPASS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetByPassResponse(res);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string GetZonesStatus(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.ZONES_STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetZonesResponse(res);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string GetMemoryStatus(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.MEMORY_STATUS, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetZonesListResponse(res, COMMANDS.MEMORY_STATUS);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private string GetByPassResponse(RestResponse response)
        {
            var content = response.Content;
            if (content.Contains("DISCONECTED"))
                return "DST";
            if (content.Contains("BYP:"))
            {
                var subs = content.Substring(content.IndexOf("BYP:") + 4);
                subs = subs.Remove(subs.IndexOf("\""));
                return subs;
            }
            else
                return GetStatus(content);
        }

        private string GetZonesResponse(RestResponse response)
        {
            var content = response.Content;
            if (content.Contains("DISCONECTED"))
                return "DST";
            if (content.Contains("STSZ:"))
            {
                var subs = content.Substring(content.IndexOf("STSZ:") + 5);
                subs = subs.Remove(subs.IndexOf("\""));
                var zones = subs.Split(',');
                string res = "";
                foreach (var item in zones)
                {
                    if (item.Length < 2)
                        res = res + "0" + item + ",";
                    else
                        res = res + item + ",";
                }
                res = res.Remove(res.LastIndexOf(","));
                return res;
            }
            else
                return GetStatus(content);
        }

        private string GetZonesListResponse(RestResponse response, string command)
        {
            var content = response.Content;
            if (content.Contains("DISCONECTED"))
                return "DST";
            var header = command + ":";
            if (content.Contains(header))
            {
                var subs = content.Substring(content.IndexOf(header) + header.Length);
                subs = subs.Remove(subs.IndexOf("\""));
                var zones = subs.Split(',');
                string res = "";
                foreach (var item in zones)
                {
                    if (item.Length < 2)
                        res = res + "0" + item + ",";
                    else
                        res = res + item + ",";
                }
                res = res.Remove(res.LastIndexOf(","));
                return res;
            }
            else
                return GetStatus(content);
        }

        private string GetVersion(RestResponse response)
        {
            var content = response.Content;
            var res = content.Substring(content.IndexOf("VER:") + 4);
            res = res.Remove(res.IndexOf("\"")).Replace(",", ".");
            return res;
        }

        private string GetStatus(RestResponse response)
        {
            return GetStatus(response.Content);
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

        public void ConfigurePublicKey(ushort key, string accessToken, long deviceId)
        {
            var hex = key.ToString("X4");
            SendCommand("PGR059:" + hex, accessToken, deviceId);
            SendCommand("PGR059:2,5,8", accessToken, deviceId);
        }

        public string SendCommand(string command, string accessToken, long deviceId)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(command, accessToken, deviceId);
                var res = Execute(serviceUrl, MethodName, request);
                return GetStatus(res);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public bool SendGetPGMCommand(string accessToken, long deviceId, int pgm)
        {
            try
            {
                var command = "PGM" + pgm;
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(command, accessToken, deviceId);
                var res = Execute(serviceUrl, MethodName, request);
                var sendCommandResult = JsonConvert.DeserializeObject<SendCommandResultWraper>(res.Content);
                return sendCommandResult.SendCommandResult.Text.Contains("PGM" + pgm + ":1");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public BatteryStateResponse SendGetBatteryStatusCommand(string accessToken, long deviceId)
        {
            try
            {
                var response = new BatteryStateResponse();
                var command = "STSB";
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(command, accessToken, deviceId);
                var res = Execute(serviceUrl, MethodName, request);
                var sendCommandResult = JsonConvert.DeserializeObject<SendCommandResultWraper>(res.Content);
                var text = sendCommandResult.SendCommandResult.Text;
                if (text.Contains(":") && text.Contains(","))
                {
                    text = text.Split(':')[1];
                    var fields = text.Split(',');
                    response.InTension = GetBatteryField(fields[0]);
                    response.ChargeTension = GetBatteryField(fields[1]);
                    response.TestTension = GetBatteryField(fields[2]);
                    response.Current = float.Parse(fields[3].Substring(1));
                }
                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private float GetBatteryField(string text)
        {
            return float.Parse(text.Substring(1)) / 10;
        }

        public string SendInstallerCommand(string command, string accessToken, long deviceId)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(command, accessToken, deviceId);
                var res = Execute(serviceUrl, MethodName, request);
                var sendCommandResult = JsonConvert.DeserializeObject<SendCommandResultWraper>(res.Content);
                return sendCommandResult.SendCommandResult.Text;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string SendCommandToIdentifier(string command, string identifier, string pin = "")
        {
            try
            {
                command = "INST-" + command;
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequestIdentifier(command, identifier, pin);
                var res = Execute(serviceUrl, MethodName, request);
                var sendCommandResult = JsonConvert.DeserializeObject<SendCommandResultWraper>(res.Content);
                return sendCommandResult.SendCommandResult.Text;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private RestResponse Execute(string serviceUrl, string methodName, RestRequest request)
        {
            RestResponse response = null;
            var url = serviceUrl + "/" + methodName;
            try
            {
                request.AddHeader(SystemAuth.HeaderName, SystemAuth.ConfiguredKey);
                var client = new RestClient(url)
                {
                    Timeout = 20000
                };
                response = (RestResponse)client.Execute(request);
            }
            catch (Exception e)
            {
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.Content = e.ToString();
            }
            return response;
        }

        private RestRequest CreateRequest(string command, string accessToken, long deviceId)
        {
            AccountBusiness acBz = new AccountBusiness();
            var account = acBz.GetAccount(accessToken);
            var device = account.Devices.Find(x => x.DeviceId == deviceId);
            var req = new RestRequest(Method.POST) { RequestFormat = DataFormat.Json };
            req.AddJsonBody(new { cmd = command, identifier = device.Mac, pin = device.Pin });
            return req;
        }
        private RestRequest CreateRequest(ExclusionRequest parameters, string command)
        {
            string accessToken = parameters.AccessToken;
            long deviceId = parameters.DeviceId;
            AccountBusiness acBz = new AccountBusiness();
            var account = acBz.GetAccount(accessToken);
            var device = account.Devices.Find(x => x.DeviceId == deviceId);
            var req = new RestRequest(Method.POST) { RequestFormat = DataFormat.Json };
            req.AddJsonBody(new { cmd = command, identifier = device.Mac, pin = device.Pin, zones = parameters.Zones });
            return req;
        }

        private RestRequest CreateRequest(PGMRequest parameters, string command)
        {
            string accessToken = parameters.AccessToken;
            long deviceId = parameters.DeviceId;
            AccountBusiness acBz = new AccountBusiness();
            var account = acBz.GetAccount(accessToken);
            var device = account.Devices.Find(x => x.DeviceId == deviceId);
            var req = new RestRequest(Method.POST) { RequestFormat = DataFormat.Json };
            req.AddJsonBody(new { cmd = command, identifier = device.Mac, pin = device.Pin, zone = parameters.Zone, state = parameters.State });
            return req;
        }

        private RestRequest CreateRequestIdentifier(string command, string deviceIdentifier, string pin)
        {
            var req = new RestRequest(Method.POST) { RequestFormat = DataFormat.Json };
            req.AddJsonBody(new { cmd = command, identifier = deviceIdentifier, pin = pin });
            return req;
        }

        public DateTime GetTimeCommand(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var request = CreateRequest(COMMANDS.CLOCK, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                var sendCommandResult = JsonConvert.DeserializeObject<SendCommandResultWraper>(res.Content);
                var timeText = sendCommandResult.SendCommandResult.Text.Remove(0, 4).Trim();
                var time = DateTime.ParseExact(timeText, "HHmmss,ddMMyy", CultureInfo.InvariantCulture);
                return time;
            }
            catch (Exception e) { throw e; }
        }

        public DateTime SyncCommand(CommandRequest commandRequest)
        {
            try
            {
                var serviceUrl = ConfigurationManager.AppSettings["PrivateService"];
                var MethodName = "SendCommand";
                var serverTime = GetCurrentDateTime();
                var cmd = COMMANDS.CLOCK + ":" + serverTime.ToString("HHmmss,ddMMyy", CultureInfo.InvariantCulture);
                var request = CreateRequest(cmd, commandRequest.AccessToken, commandRequest.DeviceId);
                var res = Execute(serviceUrl, MethodName, request);
                var sendCommandResult = JsonConvert.DeserializeObject<SendCommandResultWraper>(res.Content);
                var timeText = sendCommandResult.SendCommandResult.Text.Remove(0, 4).Trim();
                var time = DateTime.ParseExact(timeText, "HHmmss,ddMMyy", CultureInfo.InvariantCulture);
                return time;
            }
            catch (Exception e) { throw e; }
        }

        public DateTime GetCurrentDateTime()
        {
            var res = DateTime.Now.AddHours(-3);
            return res;
        }

    }
}
