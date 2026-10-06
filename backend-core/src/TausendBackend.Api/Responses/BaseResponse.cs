using TausendBackend.Api.Enums;

namespace TausendBackend.Api.Responses
{
    public abstract class BaseResponse
    {
        public ResponseStates State { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }

        protected BaseResponse()
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
