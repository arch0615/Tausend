using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Tausend.Backend.Models;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Responses.DeviceService;
using Tausend.Core.Security;
using Tausend.RelayServer.Settings;

namespace Tausend.RelayServer.Business
{
    public class ServiceBusiness
    {

        ServiceSettings ss;

        public ServiceBusiness()
        {
            ss = SettingsManager.Instance.GetServiceSettings();
        }

        public Device GetDevice(long DeviceId)
        {
            var request = CreateRequest();
            request.AddJsonBody(new { DeviceId });
            var response = Execute(ss.DeviceService, "GetDeviceByID", request);
            var result = new JavaScriptSerializer().Deserialize<DeviceResponse>(response.Content);
            return result.Device;
        }

        public Device GetDevice(string Identifier)
        {
            var request = CreateRequest();
            request.AddJsonBody(new { Identifier });
            var response = Execute(ss.DeviceService, "GetDeviceByIdentifier", request);
            var result = new JavaScriptSerializer().Deserialize<DeviceResponse>(response.Content);
            Console.WriteLine("DEVICE: \n" + response.Content);
            return result.Device;
        }

        public void NotifyEvent(string identifier, int parameter, string datetime, string eventType, int cid, int partition, int sequence)
        {
            var notification = new NotificationRequest()
            {
                AlarmIdentifier = identifier,
                AlarmParameter = parameter,
                EventDateTime = datetime,
                EventType = eventType,
                NotificationType = cid,
                Partition = partition,
                Secuence = sequence
            };
            var request = CreateRequest();
            request.AddJsonBody(notification);
            var res = Execute(ss.NotificationService, "NotifyEvent", request);
        }

        public void DisassociateDevice(string identifier)
        {
            var notification = new DisassociateRequest()
            {
                Identifier = identifier,
            };
            var request = CreateRequest();
            request.AddJsonBody(notification);
            var res = Execute(ss.DeviceService, "DissasociateCentral", request);
        }

        private RestResponse Execute(string serviceUrl, string methodName, RestRequest request)
        {
            RestResponse response = null;
            try
            {
                var url = serviceUrl + "/" + methodName;
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

        private RestRequest CreateRequest()
        {
            var req = new RestRequest(Method.POST) { RequestFormat = DataFormat.Json };
            req.AddHeader(SystemAuth.HeaderName, SystemAuth.ConfiguredKey);
            return req;
        }
    }
}
