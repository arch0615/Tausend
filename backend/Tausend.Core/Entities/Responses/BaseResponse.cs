using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Enums;

namespace Tausend.Core.Responses
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public abstract class BaseResponse
    {
        [DataMember]
        public ResponseStates State;
        [DataMember]
        public string Message;
        [DataMember]
        public int Code;

        public BaseResponse()
        {
            State = ResponseStates.OK;
            Message = "OK";
            Code = 0;
        }

        public void InformBusinessError(string message, int code)
        {
            State = ResponseStates.BUSSINESS_ERROR;
            Message = message;
            Code = code;
        }

        public void InformWrongData(string message, int code)
        {
            State = ResponseStates.WRONG_DATA;
            Message = message;
            Code = code;
        }

        public void InformNotFound(string message)
        {
            State = ResponseStates.NOT_FOUND;
            Message = message;
            Code = 404;
        }

        public void InformServerError(string message)
        {
            State = ResponseStates.SERVER_ERROR;
            Message = message;
            Code = 11111;
        }
        public void InformServerError(Exception e)
        {
            State = ResponseStates.SERVER_ERROR;
            Message = e.ToString();
            Code = 11111;
        }

        public void InformOk()
        {
            State = ResponseStates.OK;
            Message = "OK";
            Code = 0;
        }

        public void InformUnauthorized()
        {
            State = ResponseStates.UNAUTHORIZED;
            Message = "Access Token inválido";
            Code = 401;
        }

        public void InformForbidden()
        {
            State = ResponseStates.FORBIDDEN;
            Message = "No tiene permisos para realizar esta acción";
            Code = 403;
        }
    }
}