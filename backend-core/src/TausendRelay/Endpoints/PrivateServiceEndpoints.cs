using TausendRelay.Relay;
using TausendRelay.Responses;
using TausendRelay.Security;

namespace TausendRelay.Endpoints
{
    public record SendCommandRequest(string Cmd, string Identifier, string Pin);
    public record PgmCommandRequest(string Cmd, string Identifier, int Zone, bool State);
    public record BypCommandRequest(string Cmd, string Identifier, List<int> Zones);

    /// <summary>
    /// Port of backend/Tausend.UDPListener/Services/PrivateService.cs -- the backend -> relay leg
    /// of the command path (see RELAY_DECISION.md). The original was a WCF service with
    /// BodyStyle=Wrapped, meaning its JSON responses come back as {"MethodNameResult": {...}}.
    /// backend-core's CommandBusiness (TausendBackend.Api/Business/CommandBusiness.cs) already
    /// talks to that exact shape and hasn't changed, so these endpoints reproduce the same
    /// envelope on purpose -- this is the wire contract, not incidental WCF trivia.
    /// </summary>
    public static class PrivateServiceEndpoints
    {
        public static void MapPrivateService(this WebApplication app)
        {
            var group = app.MapGroup("/PrivateService");

            group.MapPost("/SendCommand", async (SendCommandRequest request, UdpRelayService relay, HttpRequest http, IConfiguration config) =>
            {
                var res = new CommandResponse();
                if (!SystemAuth.IsValidRequest(http, config))
                {
                    res.InformUnauthorized();
                    return Results.Json(new { SendCommandResult = res });
                }
                try
                {
                    res.Text = await relay.SendCommandAsync(request.Cmd, request.Identifier, request.Pin);
                }
                catch (Exception e)
                {
                    res.InformServerError(e);
                }
                return Results.Json(new { SendCommandResult = res });
            });

            group.MapPost("/PGMCommand", async (PgmCommandRequest request, UdpRelayService relay, HttpRequest http, IConfiguration config) =>
            {
                var res = new CommandResponse();
                if (!SystemAuth.IsValidRequest(http, config))
                {
                    res.InformUnauthorized();
                    return Results.Json(new { PGMCommandResult = res });
                }
                try
                {
                    await relay.SendPgmCommandAsync(request.Identifier, request.Zone, request.State);
                }
                catch (Exception e)
                {
                    res.InformServerError(e);
                }
                return Results.Json(new { PGMCommandResult = res });
            });

            group.MapPost("/BYPCommand", async (BypCommandRequest request, UdpRelayService relay, HttpRequest http, IConfiguration config) =>
            {
                var res = new CommandResponse();
                if (!SystemAuth.IsValidRequest(http, config))
                {
                    res.InformUnauthorized();
                    return Results.Json(new { BYPCommandResult = res });
                }
                try
                {
                    await relay.SendBypCommandAsync(request.Identifier, request.Zones ?? []);
                }
                catch (Exception e)
                {
                    res.InformServerError(e);
                }
                return Results.Json(new { BYPCommandResult = res });
            });
        }
    }
}
