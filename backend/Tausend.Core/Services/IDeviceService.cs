using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Tausend.Backend.Models;
using Tausend.Core.Entities;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Entities.Responses.DeviceService;
using Tausend.Core.Models;
using Tausend.Core.Responses;
using Tausend.Core.Responses.DeviceService;

namespace Tausend.Core.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "IDeviceService" en el código y en el archivo de configuración a la vez.
    [ServiceContract(Name = "DeviceService", Namespace = "http://Tausend.Wearelomo.com")]
    public interface IDeviceService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateDevice", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        NewCreatedDeviceResponse CreateDevice(CreateDeviceRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "UpdateDevice", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CreatedDeviceResponse UpdateDevice(UpdateDeviceRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetDeviceByIdentifier", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        DeviceResponse GetDeviceByIdentifier(Device device);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetDeviceByID", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        DeviceResponse GetDeviceByID(Device device);
        [OperationContract]
        [WebInvoke(UriTemplate = "DeleteDevice", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        DeletedDeviceResponse DeleteDevice(Device device);
        [OperationContract]
        [WebInvoke(UriTemplate = "UpdateDeviceConnectionParameters", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        UpdateDeviceResponse UpdateDeviceConnectionParameters(Device device);
        [OperationContract]
        [WebInvoke(UriTemplate = "UpdateDeviceLastConnection", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        UpdateDeviceResponse UpdateDeviceLastConnection(Device device);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetZones", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfZonesResponse GetZones(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateZones", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        UpdateDeviceResponse CreateZones(CreateZoneRequest zonesRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetExclusions", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfExclusionsResponse GetExclusions(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateExclusions", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        UpdateDeviceResponse CreateExclusions(CreateExclusionRequest exclusionsRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetMemory", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfZonesResponse GetMemory(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetProgramControls", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfProgramControlResponse GetProgramControls(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateProgramControls", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        UpdateDeviceResponse CreateProgramControls(PostProgramControlRequest programControlRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "EnumUsers", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfUserResponse EnumUsers(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateUsers", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        UpdateDeviceResponse CreateUsers(ListOfUsersRequest programControlRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetTime", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        TimeResponse GetTime(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "SyncTime", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        TimeResponse SyncTime(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "BlockPIN", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        SimpleResponse BlockPIN(PinRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateDeviceSMS", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CreatedDeviceResponse CreateDeviceSMS(CreateDeviceSMSRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "UpdateDeviceSMS", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CreatedDeviceResponse UpdateDeviceSMS(UpdateDeviceSMSRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "DeleteDeviceSMS", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        DeletedDeviceResponse DeleteDeviceSMS(Device device);
        [OperationContract]
        [WebInvoke(UriTemplate = "DissasociateCentral", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        DisassociateCentralResponse DisassociateCentral(DeviceDisassociate device);
    }
}
