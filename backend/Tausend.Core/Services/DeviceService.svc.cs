using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Tausend.Backend.Business;
using Tausend.Backend.Models;
using Tausend.Core.Business;
using Tausend.Core.Entities;
using Tausend.Core.Entities.Models;
using Tausend.Core.Entities.Requests;
using Tausend.Core.Entities.Responses.DeviceService;
using Tausend.Core.Enums;
using Tausend.Core.Models;
using Tausend.Core.Responses;
using Tausend.Core.Responses.DeviceService;
using Tausend.Core.Security;

namespace Tausend.Core.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "DeviceService" en el código, en svc y en el archivo de configuración a la vez.
    // NOTA: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione DeviceService.svc o DeviceService.svc.cs en el Explorador de soluciones e inicie la depuración.
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(Namespace = "http://Tausend.Wearelomo.com", InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class DeviceService : IDeviceService
    {
        public NewCreatedDeviceResponse CreateDevice(CreateDeviceRequest request)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var resp = bz.ValidatePinOnDevice(request.Identifier, request.Pin);
            if (!acbz.ValidateAccessToken(request.AccessToken))
            {
                var res = new NewCreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }

            if (resp == "ok")
            {
                return bz.CreateDevice(request.AccessToken, request.Description, request.Identifier, request.Pin, request.Email);
            }
            else if (resp == "invalid")
            {
                var res = new NewCreatedDeviceResponse();
                res.InformBusinessError("PIN incorrecto", 400);
                return res;
            }
            else
            {
                var res = new NewCreatedDeviceResponse();
                res.InformBusinessError("Central offline o inexistente", 400);
                return res;
            }
        }

        public DeletedDeviceResponse DeleteDevice(Device device)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(device.AccessToken);
            if (accountId == 0)
            {
                var res = new DeletedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return bz.DeleteDevice(device.DeviceId, accountId);
        }

        public DeviceResponse GetDeviceByID(Device device)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(device.AccessToken);
            if (accountId == 0 && !SystemAuth.IsValidRequest())
            {
                var res = new DeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            // accountId stays 0 only for a valid system (relay) caller -- GetDevice treats 0 as
            // "unscoped", everything else as "must own this device". See DAY5_SUMMARY.md.
            return bz.GetDevice(device.DeviceId, accountId);
        }

        public DeviceResponse GetDeviceByIdentifier(Device device)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(device.AccessToken);
            if (accountId == 0 && !SystemAuth.IsValidRequest())
            {
                var res = new DeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return bz.GetDevice(device.Identifier, accountId);
        }

        public CreatedDeviceResponse UpdateDevice(UpdateDeviceRequest request)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var res = new CreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }

            var pin = string.Format("0000{0}", request.Pin);
            pin = pin.Substring(pin.Length - 4);
            var resp = bz.ValidatePinOnDevice(request.Identifier, pin);
            if (resp == "ok")
            {
                return bz.UpdateDevice(request.DeviceId, request.Description, request.Identifier, pin, accountId);
            }
            else if (resp == "invalid")
            {
                var res = new CreatedDeviceResponse();
                res.InformBusinessError("PIN incorrecto", 400);
                return res;
            }
            else
            {
                var res = new CreatedDeviceResponse();
                res.InformBusinessError("Central offline o inexistente", 400);
                return res;
            }
        }

        // These two are called by the relay (Tausend.UDPListener), not by user devices -- a user
        // AccessToken doesn't apply here, so they're gated on the shared RelaySystemKey instead
        // (see Tausend.Core.Security.SystemAuth). Both sides of the relay integration must be
        // provisioned with the same key; see backend/RELAY_DECISION.md.
        public UpdateDeviceResponse UpdateDeviceConnectionParameters(Device device)
        {
            if (!SystemAuth.IsValidRequest())
            {
                var unauthorized = new UpdateDeviceResponse();
                unauthorized.InformUnauthorized();
                return unauthorized;
            }
            var bz = new DeviceBusiness();
            return bz.UpdateDeviceConnectionParameters(device.DeviceId, device.IP, device.Port);
        }

        public UpdateDeviceResponse UpdateDeviceLastConnection(Device device)
        {
            if (!SystemAuth.IsValidRequest())
            {
                var unauthorized = new UpdateDeviceResponse();
                unauthorized.InformUnauthorized();
                return unauthorized;
            }
            var bz = new DeviceBusiness();
            return bz.UpdateDeviceLastConnection(device.DeviceId);
        }

        public ListOfZonesResponse GetZones(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", commandRequest.DeviceId));
                return res;
            }
            var open = bz.GetZonesStatus(commandRequest);
            var bypass = bz.GetZonesExclusion(commandRequest);
            var state = ResponseStates.OK;
            if (open.Equals("DST") || bypass.Equals("DST"))
            {
                open = "";
                bypass = "";
                state = ResponseStates.CENTRAL_UNRESPONSIVE;
            }
            var znBz = new ZoneBusiness();
            var zones = znBz.EnumZones(commandRequest.DeviceId);
            if (!string.IsNullOrEmpty(open))
            {
                var openZ = open.Split(',');
                foreach (var item in openZ)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var number = int.Parse(item);
                        var zone = zones.Find(x => x.ZoneNumber == number);
                        if (zone != null)
                            zone.Open = true;
                    }
                }
            }
            if (!string.IsNullOrEmpty(bypass))
            {
                var bypassZ = bypass.Split(',');
                foreach (var item in bypassZ)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var number = int.Parse(item);
                        var zone = zones.Find(x => x.ZoneNumber == number);
                        if (zone != null)
                            zone.Excluded = true;
                    }
                }
            }
            return new ListOfZonesResponse(zones)
            {
                State = state,
                Code = (int)state
            };
        }

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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", zonesRequest.DeviceId));
                return res;
            }
            var znBz = new ZoneBusiness();
            var zones = zonesRequest.Zones;
            zones.ForEach(x => x.DeviceId = zonesRequest.DeviceId);
            znBz.CreateZones(zones);
            return new UpdateDeviceResponse();
        }

        public ListOfExclusionsResponse GetExclusions(CommandRequest commandRequest)
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
                    res.InformNotFound(String.Format("Dispositivo {0} no encontrado", commandRequest.DeviceId));
                    return res;
                }
                CommandBusiness bz = new CommandBusiness();
                var open = bz.GetZonesStatus(commandRequest);
                var bypass = bz.GetZonesExclusion(commandRequest);
                var state = ResponseStates.OK;
                if (open.Equals("DST") || bypass.Equals("DST"))
                {
                    open = "";
                    bypass = "";
                    state = ResponseStates.CENTRAL_UNRESPONSIVE;
                }
                var exBz = new ExclusionBusiness();
                var znBz = new ZoneBusiness();
                var zoneNames = znBz.EnumZones(commandRequest.DeviceId);
                var zones = exBz.EnumExclusions(commandRequest.DeviceId);
                if (!string.IsNullOrEmpty(open))
                {
                    var openZ = open.Split(',');
                    foreach (var item in openZ)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            var number = int.Parse(item);
                            var zone = zones.Find(x => x.ExclusionNumber == number);
                            if (zone != null)
                                zone.Open = true;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(bypass))
                {
                    var bypassZ = bypass.Split(',');
                    foreach (var item in bypassZ)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            var number = int.Parse(item);
                            var zone = zones.Find(x => x.ExclusionNumber == number);
                            if (zone != null)
                                zone.Excluded = true;
                        }
                    }
                }
                zones.ForEach(exclusion =>
                {
                    // changes exclusionName for zoneName
                    var zone = zoneNames.Find(zn => zn.ZoneNumber == exclusion.ExclusionNumber);
                    if (zone != null)
                    {
                        exclusion.Name = zone.Name;
                    }
                });
                return new ListOfExclusionsResponse(zones)
                {
                    State = state,
                    Code = (int)state
                };
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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", exclusionsRequest.DeviceId));
                return res;
            }
            var exBz = new ExclusionBusiness();
            var exclusions = exclusionsRequest.Exclusions;
            exclusions.ForEach(x => x.DeviceId = exclusionsRequest.DeviceId);
            exBz.CreateExclusions(exclusions);
            return new UpdateDeviceResponse();
        }

        public ListOfZonesResponse GetMemory(CommandRequest commandRequest)
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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", commandRequest.DeviceId));
                return res;
            }
            CommandBusiness bz = new CommandBusiness();
            var state = ResponseStates.OK;
            var memory = bz.GetMemoryStatus(commandRequest);
            if (string.IsNullOrEmpty(memory))
            {
                return new ListOfZonesResponse(new List<Zone>())
                {
                    State = state,
                    Code = (int)state
                };
            }
            var open = bz.GetZonesStatus(commandRequest);
            if (open.Equals("DST") || memory.Equals("DST"))
            {
                open = "";
                memory = "";
                state = ResponseStates.CENTRAL_UNRESPONSIVE;
            }
            var znBz = new ZoneBusiness();
            var zones = znBz.EnumZones(commandRequest.DeviceId);
            if (!string.IsNullOrEmpty(open))
            {
                var openZ = open.Split(',');
                foreach (var item in openZ)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var number = int.Parse(item);
                        var zone = zones.Find(x => x.ZoneNumber == number);
                        if (zone != null)
                            zone.Open = true;
                    }
                }
            }
            if (!string.IsNullOrEmpty(memory))
            {
                var memoryZ = memory.Split(',');
                var memoryList = new List<int>();
                foreach (var item in memoryZ)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var number = int.Parse(item);
                        memoryList.Add(number);
                    }
                }
                zones.RemoveAll(x => !memoryList.Contains(x.ZoneNumber));
            }
            return new ListOfZonesResponse(zones)
            {
                State = state,
                Code = (int)state
            };
        }

        public ListOfProgramControlResponse GetProgramControls(CommandRequest commandRequest)
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
                    res.InformNotFound(String.Format("Dispositivo {0} no encontrado", commandRequest.DeviceId));
                    return res;
                }
                var pgmbz = new ProgramControlBusiness();
                var programControls = pgmbz.EnumProgramControls(commandRequest.DeviceId, commandRequest.AccessToken);
                return new ListOfProgramControlResponse(programControls)
                {
                    State = ResponseStates.OK,
                    Code = (int)ResponseStates.OK
                };
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

        public UpdateDeviceResponse CreateProgramControls(PostProgramControlRequest programControlRequest)
        {
            var pgmbz = new ProgramControlBusiness();
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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", programControlRequest.DeviceId));
                return res;
            }
            var programControls = programControlRequest.ProgramControls;
            programControls.ForEach(x => x.DeviceId = programControlRequest.DeviceId);
            pgmbz.CreateProgramControls(programControls);
            return new UpdateDeviceResponse();
        }

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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", commandRequest.DeviceId));
                return res;
            }
            return new ListOfUserResponse(new UserBusiness().EnumUsers(commandRequest.DeviceId));
        }

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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", usersRequest.DeviceId));
                return res;
            }
            usersRequest.Users.ForEach(x => x.DeviceId = usersRequest.DeviceId);
            new UserBusiness().CreateUsers(usersRequest.Users);
            return new UpdateDeviceResponse();
        }

        public TimeResponse GetTime(CommandRequest commandRequest)
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
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", commandRequest.DeviceId));
                return res;
            }
            var cmdBz = new CommandBusiness();
            try
            {
                res.DeviceTime = cmdBz.GetTimeCommand(commandRequest);
                res.ServerTime = cmdBz.GetCurrentDateTime();
            }
            catch (Exception e)
            {
                res.State = ResponseStates.CENTRAL_UNRESPONSIVE;
                res.Code = (int)res.State;
                res.Message = "La central no respondió";
            }
            return res;
        }

        public TimeResponse SyncTime(CommandRequest commandRequest)
        {
            var cmdBz = new CommandBusiness();
            var res = new TimeResponse();
            var accountId = AuthorizeDeviceAccess(commandRequest.AccessToken, commandRequest.DeviceId, out var hasAccess);
            if (accountId == 0)
            {
                res.InformUnauthorized();
                return res;
            }
            if (!hasAccess)
            {
                res.InformNotFound(String.Format("Dispositivo {0} no encontrado", commandRequest.DeviceId));
                return res;
            }
            try
            {
                res.DeviceTime = cmdBz.SyncCommand(commandRequest);
                res.ServerTime = cmdBz.GetCurrentDateTime();
            }
            catch (Exception e)
            {
                res.State = ResponseStates.CENTRAL_UNRESPONSIVE;
                res.Code = (int)res.State;
                res.Message = "La central no respondió";
            }
            return res;
        }

        public SimpleResponse BlockPIN(PinRequest request)
        {
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var unauthorized = new SimpleResponse();
                unauthorized.InformUnauthorized();
                return unauthorized;
            }

            SimpleResponse res;
            var bz = new DeviceBusiness();
            switch (request.Action)
            {
                case "Block":
                    res = bz.BlockDevice(request.Identifier, accountId);
                    break;
                case "Reset":
                    res = bz.ResetDevice(request.Identifier, accountId);
                    break;
                default:
                    res = new SimpleResponse();
                    res.InformBusinessError("La acción indicada no es correcta", 400);
                    break;
            }
            return res;
        }

        public CreatedDeviceResponse CreateDeviceSMS(CreateDeviceSMSRequest request)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();

            if (!acbz.ValidateAccessToken(request.AccessToken))
            {
                var res = new CreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }

            return bz.CreateDeviceSMS(request.AccessToken, request.Identifier, request.Description, request.DevicePin, request.SimPin, request.PhoneNumber, request.DeviceType);
        }

        public CreatedDeviceResponse UpdateDeviceSMS(UpdateDeviceSMSRequest request)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(request.AccessToken);
            if (accountId == 0)
            {
                var res = new CreatedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return bz.UpdateDeviceSMS(request.DeviceId, request.Identifier, request.Description, request.DevicePin, request.SimPin, request.PhoneNumber, request.DeviceType, accountId);
        }

        public DeletedDeviceResponse DeleteDeviceSMS(Device device)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(device.AccessToken);
            if (accountId == 0)
            {
                var res = new DeletedDeviceResponse();
                res.InformUnauthorized();
                return res;
            }
            return bz.DeleteDeviceSMS(device.DeviceId, accountId);
        }

        public DisassociateCentralResponse DisassociateCentral(DeviceDisassociate device)
        {
            var bz = new DeviceBusiness();
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(device.AccessToken);
            if (accountId == 0 && !SystemAuth.IsValidRequest())
            {
                var res = new DisassociateCentralResponse();
                res.InformUnauthorized();
                return res;
            }
            // accountId stays 0 only for a valid system (relay) caller -- DeviceDisassociate
            // treats 0 as "unlink everyone" (factory reset), anything else as "unlink just me".
            return bz.DeviceDisassociate(device.Identifier, accountId);
        }

        /// <summary>
        /// Resolves the caller's account and checks they either own the target device or are
        /// an Admin -- the same "not found or not yours" IDOR protection Day 5 added to device
        /// CRUD (DeleteDevice/UpdateDevice/GetDeviceByID/etc.), extended here to the
        /// zones/exclusions/PGM/users/time sub-resource endpoints below, which never got it:
        /// they used to accept any valid login regardless of whether the caller owned the
        /// device. The Admin bypass is what the cross-fleet admin dashboard (Day 7) relies on
        /// to manage any panel's data, not just an admin's own.
        /// </summary>
        /// <param name="accessToken">The caller's access token.</param>
        /// <param name="deviceId">The device being accessed.</param>
        /// <param name="hasAccess">True if the caller owns the device or is an Admin. Only
        /// meaningful when the return value is non-zero (i.e. the token itself was valid).</param>
        /// <returns>The resolved account id, or 0 if the token is missing/invalid/expired.</returns>
        private long AuthorizeDeviceAccess(string accessToken, long deviceId, out bool hasAccess)
        {
            var acbz = new AccountBusiness();
            var accountId = acbz.ResolveAccountId(accessToken);
            if (accountId == 0)
            {
                hasAccess = false;
                return 0;
            }
            var deviceBz = new DeviceBusiness();
            var owns = deviceBz.GetDevice(deviceId, accountId).Device != null;
            hasAccess = owns || acbz.IsAdmin(accessToken);
            return accountId;
        }
    }
}
