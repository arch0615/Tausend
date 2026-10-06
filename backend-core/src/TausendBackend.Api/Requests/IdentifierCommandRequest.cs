namespace TausendBackend.Api.Requests
{
    public class IdentifierCommandRequest
    {
        public string Identifier { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string Command { get; set; } = "";
        public string Pin { get; set; } = "";
    }
}
