using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "INotificationService" en el código y en el archivo de configuración a la vez.
    [ServiceContract]
    public interface INotificationService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "SendTestNotification", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped)]
        CommandResponse SendTestNotification(String DeviceToken);
        [OperationContract]
        [WebInvoke(UriTemplate = "NotifyEvent", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        CommandResponse NotifyEvent(NotificationRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "EnumEvents", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        ListOfEventsResponse EnumEvents(CommandRequest request);
    }
}
