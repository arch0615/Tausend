namespace TausendRelay.Responses
{
    /// <summary>
    /// Mirrors TausendBackend.Api's ResponseStates numbering so a caller reading both backend and
    /// relay responses sees consistent codes.
    /// </summary>
    public enum ResponseState
    {
        Ok = 0,
        ServerError = 1,
        UnauthorizedState = 3,
    }

    /// <summary>
    /// Port of backend/Tausend.UDPListener/Entities/Responses/CommandResponse.cs (+ its BaseResponse
    /// parent). The original's BaseResponse never defined InformUnauthorized() despite
    /// PrivateService.cs calling it on every endpoint -- the relay project as committed doesn't
    /// actually compile. This is a complete, working replacement.
    /// </summary>
    public class CommandResponse
    {
        public ResponseState State { get; private set; } = ResponseState.Ok;
        public string Message { get; private set; } = "OK";
        public int Code { get; private set; }
        public string Text { get; set; } = "";

        public void InformUnauthorized()
        {
            State = ResponseState.UnauthorizedState;
            Message = "Access Token inválido";
            Code = 401;
        }

        public void InformServerError(Exception e)
        {
            State = ResponseState.ServerError;
            Message = e.ToString();
            Code = 11111;
        }
    }
}
