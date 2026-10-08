using Microsoft.AspNetCore.Mvc;
using TausendBackend.Api.Business;
using TausendBackend.Api.Enums;
using TausendBackend.Api.Models;
using TausendBackend.Api.Requests;
using TausendBackend.Api.Responses;
using TausendBackend.Api.Security;

namespace TausendBackend.Api.Controllers
{
    // Routes intentionally mirror the old WCF UriTemplates (/DeviceService/{Op}).
    [ApiController]
    [Route("DeviceService")]
    public class DeviceController : ControllerBase
    {
        private readonly DeviceBusiness _deviceBusiness;
        private readonly AccountBusiness _accountBusiness;
        private readonly CommandBusiness _commandBusiness;
        private readonly ZoneBusiness _zoneBusiness;
        private readonly ExclusionBusiness _exclusionBusiness;
        private readonly ProgramControlBusiness _programControlBusiness;
        private readonly UserBusiness _userBusiness;
        private readonly NotificationBusiness _notificationBusiness;
        private readonly ScheduledPgmActionBusiness _scheduledPgmActionBusiness;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(
            DeviceBusiness deviceBusiness,
            AccountBusiness accountBusiness,
            CommandBusiness commandBusiness,
            ZoneBusiness zoneBusiness,
            ExclusionBusiness exclusionBusiness,
            ProgramControlBusiness programControlBusiness,
            UserBusiness userBusiness,
            NotificationBusiness notificationBusiness,
            ScheduledPgmActionBusiness scheduledPgmActionBusiness,
            IConfiguration configuration,
            ILogger<DeviceController> logger)
        {
            _deviceBusiness = deviceBusiness;
            _accountBusiness = accountBusiness;
            _commandBusiness = commandBusiness;
            _zoneBusiness = zoneBusiness;
            _exclusionBusiness = exclusionBusiness;
            _programControlBusiness = programControlBusiness;
            _userBusiness = userBusiness;
            _scheduledPgmActionBusiness = scheduledPgmActionBusiness;
            _notificationBusiness = notificationBusiness;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("CreateDevice")]
        public async Task<NewCreatedDeviceResponse> CreateDevice(CreateDeviceRequest request)
        {
            var pinCheck = await _deviceBusiness.ValidatePinOnDevice(request.Identifier, request.Pin);
            if (!_accountBusiness.ValidateAccessToken(request.AccessToken))
            {
                var res = new NewCreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            if (pinCheck == "ok")
            {
                var created = _deviceBusiness.CreateDevice(request.AccessToken, request.Description, request.Identifier, request.Pin);
                if (created.Code == 0)
                {
                    // Must never fail the request: by this point the device row and the account
                    // link are already committed, so letting this throw returned an error for a
                    // pairing that actually succeeded. The user then retried and got "Dispositivo
                    // con descripcion repetida" against the device they had just created without
                    // knowing it -- the client's issue #15, on an account they were sure had no
                    // panels. Two real ways this throws today: TimeZoneInfo.FindSystemTimeZoneById
                    // has no tzdata on a slim Linux image, and the FCM OAuth step opens
                    // Firebase:CredentialsPath, which points at a file that does not exist yet
                    // (see mobile/src/notifications/README.md) and rethrows when it is missing.
                    try
                    {
                        SendDeviceConnectedNotification(request.Identifier, request.Email);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Device {Identifier} was created but its 'device connected' notification could not be sent.", request.Identifier);
                    }
                }
                return created;
            }
            if (pinCheck == "invalid")
            {
                var res = new NewCreatedDeviceResponse();
                res.InformBusinessError("PIN incorrecto", 400);
                return res;
            }
            var offline = new NewCreatedDeviceResponse();
            offline.InformBusinessError("Central offline o inexistente", 400);
            return offline;
        }

        /// <summary>Moved out of DeviceBusiness.CreateDevice -- see that method's doc comment for
        /// why (avoiding a circular DI dependency).</summary>
        private void SendDeviceConnectedNotification(string identifier, string? email)
        {
            // IANA id, not the Windows "Argentina Standard Time" id -- this runs on Linux in
            // production, where FindSystemTimeZoneById only resolves IANA/tzdata names.
            var timeInfo = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
            var date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeInfo);
            _notificationBusiness.ProcessNotificationRequest(new NotificationRequest
            {
                Secuence = 7,
                EventDateTime = date.ToString("dd-MM-yy HH:mm:ss"),
                EventType = "E",
                NotificationType = 631,
                Partition = 0,
                AlarmParameter = 0,
                AlarmIdentifier = identifier,
                Email = email
            });
        }

        [HttpPost("DeleteDevice")]
        public DeletedDeviceResponse DeleteDevice(Device device)
        {
            var accountId = _accountBusiness.ResolveAccountId(device.AccessToken ?? "");
            if (accountId == 0)
            {
                var res = new DeletedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.DeleteDevice(device.DeviceId, accountId);
        }

        [HttpPost("GetDeviceByID")]
        public DeviceResponse GetDeviceByID(Device device)
        {
            var accountId = _accountBusiness.ResolveAccountId(device.AccessToken ?? "");
            if (accountId == 0 && !SystemAuth.IsValidRequest(Request, _configuration))
            {
                var res = new DeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.GetDevice(device.DeviceId, accountId);
        }

        [HttpPost("GetDeviceByIdentifier")]
        public DeviceResponse GetDeviceByIdentifier(Device device)
        {
            var accountId = _accountBusiness.ResolveAccountId(device.AccessToken ?? "");
            if (accountId == 0 && !SystemAuth.IsValidRequest(Request, _configuration))
            {
                var res = new DeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.GetDevice(device.Identifier ?? "", accountId);
        }

        [HttpPost("UpdateDevice")]
        public async Task<CreatedDeviceResponse> UpdateDevice(UpdateDeviceRequest request)
        {
            var accountId = _accountBusiness.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var res = new CreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            var pin = string.Format("0000{0}", request.Pin);
            pin = pin.Substring(pin.Length - 4);
            var pinCheck = await _deviceBusiness.ValidatePinOnDevice(request.Identifier, pin);
            if (pinCheck == "ok")
            {
                return _deviceBusiness.UpdateDevice(request.DeviceId, request.Description, request.Identifier, pin, accountId);
            }
            if (pinCheck == "invalid")
            {
                var res = new CreatedDeviceResponse();
                res.InformBusinessError("PIN incorrecto", 400);
                return res;
            }
            var offline = new CreatedDeviceResponse();
            offline.InformBusinessError("Central offline o inexistente", 400);
            return offline;
        }

        // Relay-only (Tausend.UDPListener) -- see backend/RELAY_DECISION.md and DAY5_SUMMARY.md.
        [HttpPost("UpdateDeviceConnectionParameters")]
        public UpdateDeviceResponse UpdateDeviceConnectionParameters(Device device)
        {
            if (!SystemAuth.IsValidRequest(Request, _configuration))
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.UpdateDeviceConnectionParameters(device.DeviceId, device.IP ?? "", device.Port ?? "");
        }

        [HttpPost("UpdateDeviceLastConnection")]
        public UpdateDeviceResponse UpdateDeviceLastConnection(Device device)
        {
            if (!SystemAuth.IsValidRequest(Request, _configuration))
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.UpdateDeviceLastConnection(device.DeviceId);
        }

        [HttpPost("BlockPIN")]
        public SimpleResponse BlockPIN(PinRequest request)
        {
            var accountId = _accountBusiness.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var unauthorized = new SimpleResponse();
                unauthorized.InformUnauthorized();
                return unauthorized;
            }

            return request.Action switch
            {
                "Block" => _deviceBusiness.BlockDevice(request.Identifier, accountId),
                "Reset" => _deviceBusiness.ResetDevice(request.Identifier, accountId),
                _ => WrongAction()
            };

            SimpleResponse WrongAction()
            {
                var r = new SimpleResponse();
                r.InformBusinessError("La acción indicada no es correcta", 400);
                return r;
            }
        }

        [HttpPost("CreateDeviceSMS")]
        public CreatedDeviceResponse CreateDeviceSMS(CreateDeviceSMSRequest request)
        {
            if (!_accountBusiness.ValidateAccessToken(request.AccessToken))
            {
                var res = new CreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.CreateDeviceSMS(request.AccessToken, request.Identifier, request.Description, request.DevicePin, request.SimPin, request.PhoneNumber, request.DeviceType);
        }

        [HttpPost("UpdateDeviceSMS")]
        public CreatedDeviceResponse UpdateDeviceSMS(UpdateDeviceSMSRequest request)
        {
            var accountId = _accountBusiness.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var res = new CreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.UpdateDeviceSMS(request.DeviceId, request.Identifier, request.Description, request.DevicePin, request.SimPin, request.PhoneNumber, request.DeviceType, accountId);
        }

        [HttpPost("DeleteDeviceSMS")]
        public DeletedDeviceResponse DeleteDeviceSMS(Device device)
        {
            var accountId = _accountBusiness.ResolveAccountId(device.AccessToken ?? "");
            if (accountId == 0)
            {
                var res = new DeletedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return _deviceBusiness.DeleteDeviceSMS(device.DeviceId, accountId);
        }

        [HttpPost("DissasociateCentral")]
        public DisassociateCentralResponse DisassociateCentral(DeviceDisassociate device)
        {
            var accountId = _accountBusiness.ResolveAccountId(device.AccessToken ?? "");
            if (accountId == 0 && !SystemAuth.IsValidRequest(Request, _configuration))
            {
                var res = new DisassociateCentralResponse();
                res.InformUnauthorized();
                return res;
            }
            // accountId stays 0 only for a valid system (relay) caller -- DeviceDisassociate
            // treats 0 as "unlink everyone" (factory reset), anything else as "unlink just me".
            return _deviceBusiness.DeviceDisassociate(device.Identifier ?? "", accountId);
        }

        [HttpPost("GetZones")]
        public async Task<ListOfZonesResponse> GetZones(CommandRequest commandRequest)
        {
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new ListOfZonesResponse(null);
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new ListOfZonesResponse(null);
                res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                return res;
            }
            var open = await _commandBusiness.GetZonesStatus(commandRequest);
            var bypass = await _commandBusiness.GetZonesExclusion(commandRequest);
            var state = ResponseStates.OK;
            if (open.Equals("DST") || bypass.Equals("DST"))
            {
                open = "";
                bypass = "";
                state = ResponseStates.CENTRAL_UNRESPONSIVE;
            }
            var zones = _zoneBusiness.EnumZones(commandRequest.DeviceId);
            ApplyOpenAndBypass(zones, open, bypass, z => z.ZoneNumber, (z, v) => z.Open = v, (z, v) => z.Excluded = v);
            return new ListOfZonesResponse(zones) { State = state, Code = (int)state };
        }

        [HttpPost("CreateZones")]
        public UpdateDeviceResponse CreateZones(CreateZoneRequest zonesRequest)
        {
            var accountId = AuthorizeDeviceAccess(zonesRequest.AccessToken, zonesRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new UpdateDeviceResponse();
                res.InformNotFound($"Dispositivo {zonesRequest.DeviceId} no encontrado");
                return res;
            }
            zonesRequest.Zones.ForEach(x => x.DeviceId = zonesRequest.DeviceId);
            _zoneBusiness.CreateZones(zonesRequest.Zones);
            return new UpdateDeviceResponse();
        }

        [HttpPost("GetExclusions")]
        public async Task<ListOfExclusionsResponse> GetExclusions(CommandRequest commandRequest)
        {
            try
            {
                var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
                if (accountId == 0)
                {
                    var res = new ListOfExclusionsResponse(null);
                    res.InformUnauthorized();
                    return res;
                }
                if (!hasAccess)
                {
                    var res = new ListOfExclusionsResponse(null);
                    res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                    return res;
                }
                var open = await _commandBusiness.GetZonesStatus(commandRequest);
                var bypass = await _commandBusiness.GetZonesExclusion(commandRequest);
                var state = ResponseStates.OK;
                if (open.Equals("DST") || bypass.Equals("DST"))
                {
                    open = "";
                    bypass = "";
                    state = ResponseStates.CENTRAL_UNRESPONSIVE;
                }
                var zoneNames = _zoneBusiness.EnumZones(commandRequest.DeviceId);
                var exclusions = _exclusionBusiness.EnumExclusions(commandRequest.DeviceId);
                ApplyOpenAndBypass(exclusions, open, bypass, e => e.ExclusionNumber, (e, v) => e.Open = v, (e, v) => e.Excluded = v);
                exclusions.ForEach(exclusion =>
                {
                    // changes exclusionName for zoneName
                    var zone = zoneNames.Find(zn => zn.ZoneNumber == exclusion.ExclusionNumber);
                    if (zone != null)
                        exclusion.Name = zone.Name;
                });
                return new ListOfExclusionsResponse(exclusions) { State = state, Code = (int)state };
            }
            catch (Exception e)
            {
                return new ListOfExclusionsResponse(new List<Exclusion>())
                {
                    State = ResponseStates.SERVER_ERROR,
                    Code = 500,
                    Message = e.Message
                };
            }
        }

        [HttpPost("CreateExclusions")]
        public UpdateDeviceResponse CreateExclusions(CreateExclusionRequest exclusionsRequest)
        {
            var accountId = AuthorizeDeviceAccess(exclusionsRequest.AccessToken, exclusionsRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new UpdateDeviceResponse();
                res.InformNotFound($"Dispositivo {exclusionsRequest.DeviceId} no encontrado");
                return res;
            }
            exclusionsRequest.Exclusions.ForEach(x => x.DeviceId = exclusionsRequest.DeviceId);
            _exclusionBusiness.CreateExclusions(exclusionsRequest.Exclusions);
            return new UpdateDeviceResponse();
        }

        [HttpPost("GetMemory")]
        public async Task<ListOfZonesResponse> GetMemory(CommandRequest commandRequest)
        {
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new ListOfZonesResponse(null);
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new ListOfZonesResponse(null);
                res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                return res;
            }
            var state = ResponseStates.OK;
            var memory = await _commandBusiness.GetMemoryStatus(commandRequest);
            if (string.IsNullOrEmpty(memory))
            {
                return new ListOfZonesResponse(new List<Zone>()) { State = state, Code = (int)state };
            }
            var open = await _commandBusiness.GetZonesStatus(commandRequest);
            if (open.Equals("DST") || memory.Equals("DST"))
            {
                open = "";
                memory = "";
                state = ResponseStates.CENTRAL_UNRESPONSIVE;
            }
            var zones = _zoneBusiness.EnumZones(commandRequest.DeviceId);
            if (!string.IsNullOrEmpty(open))
            {
                foreach (var item in open.Split(','))
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    var zone = zones.Find(x => x.ZoneNumber == int.Parse(item));
                    if (zone != null) zone.Open = true;
                }
            }
            if (!string.IsNullOrEmpty(memory))
            {
                var memoryList = new List<int>();
                foreach (var item in memory.Split(','))
                {
                    if (!string.IsNullOrEmpty(item)) memoryList.Add(int.Parse(item));
                }
                zones.RemoveAll(x => !memoryList.Contains(x.ZoneNumber));
            }
            return new ListOfZonesResponse(zones) { State = state, Code = (int)state };
        }

        [HttpPost("GetProgramControls")]
        public async Task<ListOfProgramControlResponse> GetProgramControls(CommandRequest commandRequest)
        {
            try
            {
                var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
                if (accountId == 0)
                {
                    var res = new ListOfProgramControlResponse(null);
                    res.InformUnauthorized();
                    return res;
                }
                if (!hasAccess)
                {
                    var res = new ListOfProgramControlResponse(null);
                    res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                    return res;
                }
                var programControls = await _programControlBusiness.EnumProgramControls(commandRequest.DeviceId, commandRequest.AccessToken);
                return new ListOfProgramControlResponse(programControls) { State = ResponseStates.OK, Code = (int)ResponseStates.OK };
            }
            catch (Exception e)
            {
                return new ListOfProgramControlResponse(null)
                {
                    State = ResponseStates.SERVER_ERROR,
                    Code = (int)ResponseStates.SERVER_ERROR,
                    Message = e.Message
                };
            }
        }

        [HttpPost("CreateProgramControls")]
        public UpdateDeviceResponse CreateProgramControls(PostProgramControlRequest programControlRequest)
        {
            var accountId = AuthorizeDeviceAccess(programControlRequest.AccessToken, programControlRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new UpdateDeviceResponse();
                res.InformNotFound($"Dispositivo {programControlRequest.DeviceId} no encontrado");
                return res;
            }
            programControlRequest.ProgramControls.ForEach(x => x.DeviceId = programControlRequest.DeviceId);
            _programControlBusiness.CreateProgramControls(programControlRequest.ProgramControls);
            return new UpdateDeviceResponse();
        }

        // "Scheduled Departures" per the client spec -- a recurring "at this time, on these days,
        // set this PGM output to this state" rule, fired by ScheduledPgmDispatcher (a background
        // service, see Program.cs). Previously this screen name was wired to the same immediate
        // manual PGM toggle as the plain PGM screen, with no real scheduling behind it at all.
        [HttpPost("CreateScheduledPgmAction")]
        public CreatedScheduledPgmActionResponse CreateScheduledPgmAction(CreateScheduledPgmActionRequest request)
        {
            var accountId = AuthorizeDeviceAccess(request.AccessToken, request.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new CreatedScheduledPgmActionResponse();
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new CreatedScheduledPgmActionResponse();
                res.InformNotFound($"Dispositivo {request.DeviceId} no encontrado");
                return res;
            }
            if (request.ProgramControlNumber < 1 || request.ProgramControlNumber > 8 || request.DaysOfWeekMask == 0)
            {
                var res = new CreatedScheduledPgmActionResponse();
                res.InformWrongData("Datos de programación inválidos", 400);
                return res;
            }
            var id = _scheduledPgmActionBusiness.CreateScheduledPgmAction(
                request.DeviceId, request.ProgramControlNumber, request.TimeOfDay, request.DaysOfWeekMask, request.DesiredState);
            return new CreatedScheduledPgmActionResponse { ScheduledPgmActionId = id };
        }

        [HttpPost("EnumScheduledPgmActions")]
        public ListOfScheduledPgmActionsResponse EnumScheduledPgmActions(CommandRequest commandRequest)
        {
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new ListOfScheduledPgmActionsResponse(null);
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new ListOfScheduledPgmActionsResponse(null);
                res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                return res;
            }
            return new ListOfScheduledPgmActionsResponse(_scheduledPgmActionBusiness.EnumScheduledPgmActions(commandRequest.DeviceId));
        }

        [HttpPost("DeleteScheduledPgmAction")]
        public UpdateDeviceResponse DeleteScheduledPgmAction(ScheduledPgmActionIdRequest request)
        {
            var accountId = AuthorizeDeviceAccess(request.AccessToken, request.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new UpdateDeviceResponse();
                res.InformNotFound($"Dispositivo {request.DeviceId} no encontrado");
                return res;
            }
            var rows = _scheduledPgmActionBusiness.DeleteScheduledPgmAction(request.ScheduledPgmActionId, request.DeviceId);
            var response = new UpdateDeviceResponse();
            if (rows <= 0) response.InformNotFound($"Programación {request.ScheduledPgmActionId} no encontrada");
            return response;
        }

        [HttpPost("SetScheduledPgmActionEnabled")]
        public UpdateDeviceResponse SetScheduledPgmActionEnabled(SetScheduledPgmActionEnabledRequest request)
        {
            var accountId = AuthorizeDeviceAccess(request.AccessToken, request.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new UpdateDeviceResponse();
                res.InformNotFound($"Dispositivo {request.DeviceId} no encontrado");
                return res;
            }
            var rows = _scheduledPgmActionBusiness.SetScheduledPgmActionEnabled(request.ScheduledPgmActionId, request.DeviceId, request.Enabled);
            var response = new UpdateDeviceResponse();
            if (rows <= 0) response.InformNotFound($"Programación {request.ScheduledPgmActionId} no encontrada");
            return response;
        }

        [HttpPost("EnumUsers")]
        public ListOfUserResponse EnumUsers(CommandRequest commandRequest)
        {
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new ListOfUserResponse(null);
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new ListOfUserResponse(null);
                res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                return res;
            }
            return new ListOfUserResponse(_userBusiness.EnumUsers(commandRequest.DeviceId));
        }

        [HttpPost("CreateUsers")]
        public UpdateDeviceResponse CreateUsers(ListOfUsersRequest usersRequest)
        {
            var accountId = AuthorizeDeviceAccess(usersRequest.AccessToken, usersRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                var res = new UpdateDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                var res = new UpdateDeviceResponse();
                res.InformNotFound($"Dispositivo {usersRequest.DeviceId} no encontrado");
                return res;
            }
            usersRequest.Users.ForEach(x => x.DeviceId = usersRequest.DeviceId);
            _userBusiness.CreateUsers(usersRequest.Users);
            return new UpdateDeviceResponse();
        }

        [HttpPost("GetTime")]
        public async Task<TimeResponse> GetTime(CommandRequest commandRequest)
        {
            var res = new TimeResponse();
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                return res;
            }
            try
            {
                res.DeviceTime = await _commandBusiness.GetTimeCommand(commandRequest);
                res.ServerTime = _commandBusiness.GetCurrentDateTime();
            }
            catch
            {
                res.State = ResponseStates.CENTRAL_UNRESPONSIVE;
                res.Code = (int)res.State;
                res.Message = "La central no respondió";
            }
            return res;
        }

        [HttpPost("SyncTime")]
        public async Task<TimeResponse> SyncTime(CommandRequest commandRequest)
        {
            var res = new TimeResponse();
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                res.InformNotFound($"Dispositivo {commandRequest.DeviceId} no encontrado");
                return res;
            }
            try
            {
                res.DeviceTime = await _commandBusiness.SyncCommand(commandRequest);
                res.ServerTime = _commandBusiness.GetCurrentDateTime();
            }
            catch
            {
                res.State = ResponseStates.CENTRAL_UNRESPONSIVE;
                res.Code = (int)res.State;
                res.Message = "La central no respondió";
            }
            return res;
        }

        /// <summary>
        /// Resolves the caller's account and checks they either own the target device or are
        /// an Admin -- the same "not found or not yours" IDOR protection Day 5 added to device
        /// CRUD (DeleteDevice/UpdateDevice/GetDeviceByID/etc.), extended here to the
        /// zones/exclusions/PGM/users/time sub-resource endpoints above, which never got it:
        /// they used to accept any valid login regardless of whether the caller owned the
        /// device. The Admin bypass is what the cross-fleet admin dashboard (Day 7) relies on
        /// to manage any panel's data, not just an admin's own.
        /// </summary>
        /// <param name="hasAccess">True if the caller owns the device or is an Admin. Only
        /// meaningful when the return value is non-zero (i.e. the token itself was valid).</param>
        /// <returns>The resolved account id, or 0 if the token is missing/invalid/expired.</returns>
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

        /// <summary>Shared "mark open/excluded zones from the CSV the relay returned" logic used
        /// by GetZones and GetExclusions (identical loops in the original, over Zone vs
        /// Exclusion).</summary>
        private static void ApplyOpenAndBypass<T>(List<T> items, string open, string bypass, Func<T, int> number, Action<T, bool> setOpen, Action<T, bool> setExcluded)
        {
            if (!string.IsNullOrEmpty(open))
            {
                foreach (var item in open.Split(','))
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    var match = items.Find(x => number(x) == int.Parse(item));
                    if (match != null) setOpen(match, true);
                }
            }
            if (!string.IsNullOrEmpty(bypass))
            {
                foreach (var item in bypass.Split(','))
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    var match = items.Find(x => number(x) == int.Parse(item));
                    if (match != null) setExcluded(match, true);
                }
            }
        }
    }
}
