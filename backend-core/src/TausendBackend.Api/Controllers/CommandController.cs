using Microsoft.AspNetCore.Mvc;
using TausendBackend.Api.Business;
using TausendBackend.Api.Dao;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Models;
using TausendBackend.Api.Requests;
using TausendBackend.Api.Responses;

namespace TausendBackend.Api.Controllers
{
    // Routes intentionally mirror the old WCF UriTemplates (/CommandService/{Op}).
    [ApiController]
    [Route("CommandService")]
    public class CommandController : ControllerBase
    {
        private readonly CommandBusiness _commandBusiness;
        private readonly AccountBusiness _accountBusiness;
        private readonly DeviceBusiness _deviceBusiness;
        private readonly ProgramControlDao _programControlDao;
        // Not AdminBusiness -- that would depend back on business classes that themselves depend
        // on CommandBusiness (same circular-dependency reasoning as CommandBusiness's own DeviceDao
        // injection). AdminDao's audit-log write is the only piece needed here.
        private readonly AdminDao _adminDao;

        public CommandController(CommandBusiness commandBusiness, AccountBusiness accountBusiness, DeviceBusiness deviceBusiness, ProgramControlDao programControlDao, AdminDao adminDao)
        {
            _commandBusiness = commandBusiness;
            _accountBusiness = accountBusiness;
            _deviceBusiness = deviceBusiness;
            _programControlDao = programControlDao;
            _adminDao = adminDao;
        }

        /// <summary>Mirrors DeviceController.AuthorizeDeviceAccess -- resolves the account and
        /// reports whether it owns (or is an Admin for) the given device. CommandController was
        /// missing this check entirely until Day 27 (see DAY26_REGRESSION.md finding #1): every
        /// command endpoint here used to silently no-op (send a null identifier to the relay,
        /// which comes back empty and reads as State:OK) for any DeviceId not owned by the caller,
        /// instead of rejecting it the way DeviceController's endpoints already do.</summary>
        private long AuthorizeDeviceAccess(string accessToken, long deviceId, out bool hasAccess)
        {
            var accountId = _accountBusiness.ResolveAccountId(accessToken);
            if (accountId == 0)
            {
                hasAccess = false;
                return 0;
            }
            var owns = _deviceBusiness.GetDevice(deviceId, accountId).Device != null;
            hasAccess = owns || _accountBusiness.IsAdmin(accessToken);
            return accountId;
        }

        /// <summary>True (with `response` populated) if the request should be rejected -- either
        /// the token itself is invalid/expired, or it's valid but the device isn't the caller's
        /// own. Replaces the token-only Unauthorized() check below for every handler that takes a
        /// DeviceId.</summary>
        private bool DeviceUnauthorized(string accessToken, long deviceId, out CommandResponse response)
        {
            response = new CommandResponse();
            var accountId = AuthorizeDeviceAccess(accessToken, deviceId, out var hasAccess);
            if (accountId == 0)
            {
                response.InformUnauthorized();
                return true;
            }
            if (!hasAccess)
            {
                response.InformNotFound($"Dispositivo {deviceId} no encontrado");
                return true;
            }
            return false;
        }

        // Directly uses ProgramControlDao (not ProgramControlBusiness) -- matches the original,
        // which reads/writes only the ONE PGM slot being toggled here (commandRequest.Zone),
        // not ProgramControlBusiness.EnumProgramControls' "all 8 slots, live-overlaid" behavior
        // (that's what DeviceController.GetProgramControls uses instead).
        [HttpPost("ProgramControl")]
        public async Task<ListOfProgramControlResponse> ProgramControl(PGMRequest commandRequest)
        {
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var response = new ListOfProgramControlResponse(null);
                response.InformUnauthorized();
                return response;
            }
            if (!hasAccess)
            {
                var response = new ListOfProgramControlResponse(null);
                response.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                return response;
            }
            await _commandBusiness.PGMCommand(commandRequest);
            var programControls = _programControlDao.EnumProgramControls(commandRequest.DeviceId);
            programControls.RemoveAll(x => x.ProgramControlNumber != commandRequest.Zone);
            var activated = await _commandBusiness.SendGetPGMCommand(commandRequest.AccessToken, commandRequest.DeviceId, commandRequest.Zone);
            var existing = programControls.Find(x => x.ProgramControlNumber == commandRequest.Zone);
            if (existing == null)
            {
                programControls.Add(new ProgramControl
                {
                    DeviceId = commandRequest.DeviceId,
                    Activated = activated,
                    Name = commandRequest.Zone.ToString(),
                    ProgramControlNumber = commandRequest.Zone,
                    ProgramControlId = commandRequest.Zone
                });
            }
            else
            {
                existing.Activated = activated;
            }
            return new ListOfProgramControlResponse(programControls);
        }

        private bool Unauthorized(string accessToken, out CommandResponse response)
        {
            response = new CommandResponse();
            if (_accountBusiness.ValidateAccessToken(accessToken))
                return false;
            response.InformUnauthorized();
            return true;
        }

        [HttpPost("ArmAlarm")]
        public async Task<CommandResponse> ArmAlarm(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.ArmAlarm(request);
            return res;
        }

        [HttpPost("DisarmAlarm")]
        public async Task<CommandResponse> DisarmAlarm(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.DisarmAlarm(request);
            return res;
        }

        [HttpPost("DayArmAlarm")]
        public async Task<CommandResponse> DayArmAlarm(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.DayArmAlarm(request);
            return res;
        }

        [HttpPost("NightArmAlarm")]
        public async Task<CommandResponse> NightArmAlarm(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.NightArmAlarm(request);
            return res;
        }

        [HttpPost("GetVersion")]
        public async Task<CommandResponse> GetVersion(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.GetFirmwareVersion(request);
            return res;
        }

        [HttpPost("Exclusion")]
        public async Task<CommandResponse> Exclusion(ExclusionRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.Exclusion(request);
            return res;
        }

        // FUNA -> asalto ruidoso, FUNS -> asalto silencioso, FUNP -> panico, FUNM -> emergencia
        [HttpPost("Panic")]
        public async Task<CommandResponse> Panic(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.SendCommand("FUNP", request.AccessToken, request.DeviceId);
            return res;
        }

        [HttpPost("Emergency")]
        public async Task<CommandResponse> Emergency(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.SendCommand("FUNM", request.AccessToken, request.DeviceId);
            return res;
        }

        [HttpPost("Assault")]
        public async Task<CommandResponse> Assault(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.SendCommand("FUNS", request.AccessToken, request.DeviceId);
            return res;
        }

        [HttpPost("GetGeneralStatus")]
        public async Task<CommandResponse> GetGeneralStatus(CommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            res.Text = await _commandBusiness.GetGeneralStatus(request);
            var fails = await GetFailStatus(request);
            if (fails.AC || fails.BAT || fails.BELL1 || fails.BELL2 || fails.BUS || fails.CEL || fails.CLOCK || fails.COMU || fails.TLM || fails.VAUX)
            {
                res.Text += ",FAIL";
            }
            return res;
        }

        [HttpPost("GetFailStatus")]
        public async Task<FailStatusResponse> GetFailStatus(CommandRequest request)
        {
            var accountId = AuthorizeDeviceAccess(request.AccessToken, request.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var unauthorized = new FailStatusResponse("");
                unauthorized.InformUnauthorized();
                return unauthorized;
            }
            if (!hasAccess)
            {
                var notFound = new FailStatusResponse("");
                notFound.InformNotFound($"Dispositivo {request.DeviceId} no encontrado");
                return notFound;
            }
            return new FailStatusResponse(await _commandBusiness.GetFailStatus(request));
        }

        [HttpPost("GetZonesStatus")]
        public async Task<ZonesResponse> GetZonesStatus(CommandRequest request)
        {
            var res = new ZonesResponse();
            var accountId = AuthorizeDeviceAccess(request.AccessToken, request.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                res.InformNotFound($"Dispositivo {request.DeviceId} no encontrado");
                return res;
            }
            var open = await _commandBusiness.GetZonesStatus(request);
            var bypass = await _commandBusiness.GetZonesExclusion(request);
            if (open.Equals("DST") || bypass.Equals("DST"))
            {
                open = "";
                bypass = "";
                res.State = ResponseStates.CENTRAL_UNRESPONSIVE;
                res.Code = (int)res.State;
            }
            res.Open = open;
            res.Exclusion = bypass;
            return res;
        }

        [HttpPost("SendInstallerCommand")]
        public async Task<CommandResponse> SendInstallerCommand(InstallerCommandRequest request)
        {
            if (DeviceUnauthorized(request.AccessToken, request.DeviceId, out var res)) return res;
            // Gated on owning the panel (the check just above) plus knowing the panel's own
            // installer code, which InstallerModeScreen.tsx checks against a live PRG003 read
            // before it will send anything here.
            //
            // There used to be an additional Installer/Admin account-role requirement, replacing
            // the old app's "know the panel's code" gate with an account concept. The client
            // reported that as broken in two separate items: #20, the Instalador entry had
            // disappeared for them, and #4, "es necesario que pida clave de instalador y coincida
            // con la clave de instalador (seccion 003)". Their installers are technicians on site
            // using whatever account is on the phone and proving themselves with the panel's own
            // code, so a role nobody's account carried made the screen unreachable for everyone.
            //
            // Still enforced: the caller must own this panel or be an Admin, and every command is
            // written to the audit log below whatever the panel answers.
            var cmd = "INST-" + request.Command;
            res.Text = await _commandBusiness.SendInstallerCommand(cmd, request.AccessToken, request.DeviceId);
            // Every raw command recorded regardless of the panel's response -- see the Installer
            // Console spec's "every command recorded in audit log" requirement.
            var actorAccountId = _accountBusiness.ResolveAccountId(request.AccessToken);
            _adminDao.CreateAuditLogEntry(actorAccountId, "SendInstallerCommand", "Device", request.DeviceId, $"Command={request.Command}; Response={res.Text}");
            return res;
        }

        [HttpPost("SendCommandToIdentifier")]
        public async Task<CommandResponse> SendCommandToIdentifier(IdentifierCommandRequest request)
        {
            if (Unauthorized(request.AccessToken, out var res)) return res;
            res.Text = await _commandBusiness.SendCommandToIdentifier(request.Command, request.Identifier, request.Pin);
            return res;
        }

        [HttpPost("GetBatteryStatus")]
        public async Task<BatteryStateResponse> GetBatteryStatus(InstallerCommandRequest request)
        {
            var res = new BatteryStateResponse();
            var accountId = AuthorizeDeviceAccess(request.AccessToken, request.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                res.InformNotFound($"Dispositivo {request.DeviceId} no encontrado");
                return res;
            }
            return await _commandBusiness.SendGetBatteryStatusCommand(request.AccessToken, request.DeviceId);
        }
    }
}
