using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Entities.Responses.AdminService;
using Tausend.Core.Responses;

namespace Tausend.Core.Services
{
    [ServiceContract(Name = "AdminService", Namespace = "http://Tausend.Wearelomo.com")]
    public interface IAdminService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "EnumAllAccounts", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfAdminAccountsResponse EnumAllAccounts(AccessTokenRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "EnumAllDevices", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfAdminDevicesResponse EnumAllDevices(AccessTokenRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "SetAccountRole", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        SimpleResponse SetAccountRole(SetAccountRoleRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "BlockDevice", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        SimpleResponse BlockDevice(Tausend.Core.Models.CommandRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "ResetDevice", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        SimpleResponse ResetDevice(Tausend.Core.Models.CommandRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "DisassociateDevice", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        SimpleResponse DisassociateDevice(Tausend.Core.Models.CommandRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "EnumAuditLog", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfAuditLogResponse EnumAuditLog(AccessTokenRequest request);
    }
}
