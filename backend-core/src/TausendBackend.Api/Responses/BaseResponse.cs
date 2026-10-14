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

        /// <summary>
        /// A server fault, reported to the caller as a fixed, human-readable sentence.
        ///
        /// The raw text used to be passed straight through, which is how a user ended up staring
        /// at "Cannot insert the value NULL into column 'DeviceId', table
        /// 'TausendDB.dbo.AccountDevicePins'..." on the add-panel screen (client report, 14 Oct).
        /// Besides being meaningless to them, that hands out table and column names to anyone
        /// willing to provoke an error. The real detail goes to the server log via
        /// <see cref="ServerErrorDetail"/>, which is where it is useful.
        /// </summary>
        public const string ServerErrorMessage = "Ocurrió un error en el servidor. Intenta nuevamente.";

        /// <summary>The underlying failure, for logging only. Never serialised to the client --
        /// no public getter, so it stays out of the JSON response.</summary>
        internal string? ServerErrorDetail { get; private set; }

        public void InformServerError(string message)
        {
            State = ResponseStates.SERVER_ERROR;
            ServerErrorDetail = message;
            Message = ServerErrorMessage;
            Code = 11111;
        }

        public void InformServerError(Exception e)
        {
            State = ResponseStates.SERVER_ERROR;
            ServerErrorDetail = e.ToString();
            Message = ServerErrorMessage;
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
