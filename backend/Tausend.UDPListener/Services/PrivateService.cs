using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Tausend.Core.Responses;
using Tausend.Core.Security;

namespace Tausend.RelayServer.Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "PrivateService" en el código y en el archivo de configuración a la vez.
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(Namespace = "http://Tausend.Wearelomo.com", InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
    public class PrivateService : IPrivateService
    {
        public CommandResponse BYPCommand(string cmd, string identifier, List<int> zones)
        {
            var res = new CommandResponse();
            if (!SystemAuth.IsValidRequest())
            {
                res.InformUnauthorized();
                return res;
            }
            try
            {
                res.Text = Tausend.UDPListener.Listener.UDPListener.SendBYPCommand(identifier, zones);
            }
            catch (Exception e)
            {
                res.InformServerError(e);
            }
            return res;
        }

        public CommandResponse PGMCommand(string cmd, string identifier, int zone, bool state)
        {
            var res = new CommandResponse();
            if (!SystemAuth.IsValidRequest())
            {
                res.InformUnauthorized();
                return res;
            }
            try
            {
                res.Text = Tausend.UDPListener.Listener.UDPListener.SendPGMCommand(identifier, zone, state);
            }
            catch (Exception e)
            {
                res.InformServerError(e);
            }
            return res;
        }

        public CommandResponse SendCommand(string cmd, string identifier, string pin)
        {
            var res = new CommandResponse();
            if (!SystemAuth.IsValidRequest())
            {
                res.InformUnauthorized();
                return res;
            }
            try
            {
                res.Text = Tausend.UDPListener.Listener.UDPListener.SendCommand(cmd, identifier, pin);
            }
            catch (Exception e)
            {
                res.InformServerError(e);
            }
            return res;
        }
    }
}
