using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Tausend.Core.Entities;
using Tausend.Core.Entities.Responses.CommandService;
using Tausend.Core.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "ICommandService" en el código y en el archivo de configuración a la vez.
    [ServiceContract(Name = "CommandService", Namespace = "http://Tausend.Wearelomo.com")]
    public interface ICommandService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "ArmAlarm", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse ArmAlarm(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "DisarmAlarm", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse DisarmAlarm(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "DayArmAlarm", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse DayArmAlarm(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "NightArmAlarm", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse NightArmAlarm(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetVersion", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse GetVersion(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "ProgramControl", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfProgramControlResponse ProgramControl(PGMRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "Exclusion", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse Exclusion(ExclusionRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "Panic", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse Panic(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "Emergency", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse Emergency(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "Assault", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse Assault(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetGeneralStatus", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse GetGeneralStatus(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetFailStatus", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        FailStatusResponse GetFailStatus(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetZonesStatus", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ZonesResponse GetZonesStatus(CommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "SendInstallerCommand", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse SendInstallerCommand(InstallerCommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "SendCommandToIdentifier", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse SendCommandToIdentifier(IdentifierCommandRequest commandRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "GetBatteryStatus", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        BatteryStateResponse GetBatteryStatus(InstallerCommandRequest commandRequest);

    }
}
