using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Tausend.Backend.Business;
using Tausend.Core.Business;
using Tausend.Core.Dao;
using Tausend.Core.Entities;
using Tausend.Core.Entities.Models;
using Tausend.Core.Entities.Responses.CommandService;
using Tausend.Core.Enums;
using Tausend.Core.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "CommandService" en el código, en svc y en el archivo de configuración a la vez.
    // NOTA: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione CommandService.svc o CommandService.svc.cs en el Explorador de soluciones e inicie la depuración.
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(Namespace = "http://Tausend.Wearelomo.com", InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class CommandService : ICommandService
    {
        public CommandResponse ArmAlarm(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.ArmAlarm(commandRequest) };
        }

        //FUNA -> asalto ruidoso
        //FUNS -> asalto silencioso
        public CommandResponse Assault(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.SendCommand("FUNS", commandRequest.AccessToken, commandRequest.DeviceId) };
        }

        public CommandResponse DayArmAlarm(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.DayArmAlarm(commandRequest) };
        }

        public CommandResponse DisarmAlarm(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.DisarmAlarm(commandRequest) };
        }

        public CommandResponse Emergency(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            var res = new CommandResponse();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                res.InformUnauthorized();
                return res;
            }
            NotificationBusiness notifBz = new NotificationBusiness();
            res.Text = bz.SendCommand("FUNM", commandRequest.AccessToken, commandRequest.DeviceId);
            return res;
        }

        public CommandResponse Exclusion(ExclusionRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.Exclusion(commandRequest) };
        }

        public CommandResponse GetGeneralStatus(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            var res = new CommandResponse();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                res.InformUnauthorized();
                return res;
            }
            res.Text = bz.GetGeneralStatus(commandRequest);
            var fails = GetFailStatus(commandRequest);
            if (fails.AC || fails.BAT || fails.BELL1 || fails.BELL2 || fails.BUS || fails.CEL || fails.CLOCK || fails.COMU || fails.TLM || fails.VAUX)
            {
                res.Text += ",FAIL";
            }
            return res;
        }

        public FailStatusResponse GetFailStatus(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new FailStatusResponse("");
                res.InformUnauthorized();
                return res;
            }
            return new FailStatusResponse(bz.GetFailStatus(commandRequest));
        }

        public CommandResponse GetVersion(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.GetFirmwareVersion(commandRequest) };
        }

        public ZonesResponse GetZonesStatus(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new ZonesResponse();
                res.InformUnauthorized();
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
            return new ZonesResponse()
            {
                Open = open,
                Exclusion = bypass,
                State = state,
                Code = (int)state
            };
        }

        public ListOfZonesResponse GetZones(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new ListOfZonesResponse(null);
                res.InformUnauthorized();
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
            var openZ = open.Split(',');
            foreach (var item in openZ)
            {
                var number = int.Parse(item);
                var zone = zones.Find(x => x.ZoneNumber == number);
                zone.Open = true;
            }
            var bypassZ = bypass.Split(',');
            foreach (var item in bypassZ)
            {
                var number = int.Parse(item);
                var zone = zones.Find(x => x.ZoneNumber == number);
                zone.Excluded = true;
            }
            return new ListOfZonesResponse(zones)
            {
                State = state,
                Code = (int)state
            };
        }

        public CommandResponse NightArmAlarm(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.NightArmAlarm(commandRequest) };
        }

        public CommandResponse Panic(CommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.SendCommand("FUNP", commandRequest.AccessToken, commandRequest.DeviceId) };
        }

        public ListOfProgramControlResponse ProgramControl(PGMRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var pgmDao = new ProgramControlDao();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var response = new ListOfProgramControlResponse(null);
                response.InformUnauthorized();
                return response;
            }
            bz.PGMCommand(commandRequest);
            var ProgramControls = pgmDao.EnumProgramControls(commandRequest.DeviceId);
            ProgramControls.RemoveAll(x => x.ProgramControlNumber != commandRequest.Zone);
            var activated = bz.SendGetPGMCommand(commandRequest.AccessToken, commandRequest.DeviceId, commandRequest.Zone);
            if (!ProgramControls.Exists(x => x.ProgramControlNumber == commandRequest.Zone))
            {
                ProgramControls.Add(new ProgramControl()
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
                var pgm = ProgramControls.Find(x => x.ProgramControlNumber == commandRequest.Zone);
                pgm.Activated = activated;
            }
            var res = new ListOfProgramControlResponse(ProgramControls);
            return res;
        }

        public CommandResponse SendInstallerCommand(InstallerCommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            // Role-gated, not just hidden client-side -- the old app gated this with a
            // panel-fetched PIN instead of an account concept; the roles model replaces that.
            if (!acbz.IsInstallerOrAdmin(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformForbidden();
                return res;
            }
            var cmd = "INST-" + commandRequest.Command;
            return new CommandResponse() { Text = bz.SendInstallerCommand(cmd, commandRequest.AccessToken, commandRequest.DeviceId) };
        }

        public CommandResponse SendCommandToIdentifier(IdentifierCommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                var res = new CommandResponse();
                res.InformUnauthorized();
                return res;
            }
            return new CommandResponse() { Text = bz.SendCommandToIdentifier(commandRequest.Command, commandRequest.Identifier, commandRequest.Pin) };
        }

        public BatteryStateResponse GetBatteryStatus(InstallerCommandRequest commandRequest)
        {
            CommandBusiness bz = new CommandBusiness();
            var res = new BatteryStateResponse();
            var acbz = new AccountBusiness();
            if (!acbz.ValidateAccessToken(commandRequest.AccessToken))
            {
                res.InformUnauthorized();
                return res;
            }
            res = bz.SendGetBatteryStatusCommand(commandRequest.AccessToken, commandRequest.DeviceId);
            return res;
        }
    }
}
