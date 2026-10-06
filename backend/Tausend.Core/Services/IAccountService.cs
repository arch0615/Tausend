using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Tausend.Backend.Models;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Services
{
    [ServiceContract(Name="AccountService", Namespace = "http://Tausend.Wearelomo.com")]
    public interface IAccountService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateAccount", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountCreatedResponse CreateAccount(Account account);
        [OperationContract]
        [WebInvoke(UriTemplate = "Login", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountLoginResponse Login(Account account);
        [OperationContract]
        [WebInvoke(UriTemplate = "DeleteAccount", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountDeletedResponse DeleteAccount(Account account);
        [OperationContract]
        [WebInvoke(UriTemplate = "UpdateAccount", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountCreatedResponse UpdateAccount(UpdateAccountRequest updateRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "RecoverPassword", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountCreatedResponse RecoverPassword(Account updateRequest);
        [OperationContract]
        [WebInvoke(UriTemplate = "CreateAccountDeviceToken", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountCreatedResponse CreateAccountDeviceToken(NewDeviceToken token);
        [OperationContract]
        [WebInvoke(UriTemplate = "ValidateAccessToken", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountCreatedResponse ValidateAccessToken(AccessTokenRequest token);
        [OperationContract]
        [WebInvoke(UriTemplate = "Logout", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountDeletedResponse Logout(DeviceTokenRequest token);
        [OperationContract]
        [WebInvoke(UriTemplate = "RefreshAccessToken", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountLoginResponse RefreshAccessToken(RefreshTokenRequest request);
        [OperationContract]
        [WebInvoke(UriTemplate = "ResetPassword", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST", BodyStyle = WebMessageBodyStyle.Bare)]
        AccountCreatedResponse ResetPassword(ResetPasswordRequest request);
    }
}
