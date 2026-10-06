using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Script.Services;
using Tausend.Core.Responses;

namespace Tausend.RelayServer.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "IPrivateService" en el código y en el archivo de configuración a la vez.
    [ServiceContract(Name = "PrivateService", Namespace = "http://Tausend.Wearelomo.com")]
    public interface IPrivateService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "SendCommand", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped)]
        CommandResponse SendCommand(string cmd, string identifier, string pin);
        [OperationContract]
        [WebInvoke(UriTemplate = "PGMCommand", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped)]
        CommandResponse PGMCommand(string cmd, string identifier, int zone, bool state);
        [OperationContract]
        [WebInvoke(UriTemplate = "BYPCommand", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped)]
        CommandResponse BYPCommand(string cmd, string identifier, List<int> zones);
    }
}
