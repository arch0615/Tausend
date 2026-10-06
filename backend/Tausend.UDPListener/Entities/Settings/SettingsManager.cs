using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.RelayServer.Settings
{
    public class SettingsManager
    {
        public static SettingsManager Instance 
        { 
            get 
            {
                if (_sm == null)
                    _sm = new SettingsManager();
                return _sm;
            } 
        }
        private static SettingsManager _sm;
        private SettingsManager()
        {
        }

        NetworkSettings ns;
        ServiceSettings ss;

        public NetworkSettings GetNetworkSettings()
        {
            if (ns == null)
            {
                int p = int.Parse(ConfigurationManager.AppSettings[NetworkSettings.PORT_KEY]);
                string ip = ConfigurationManager.AppSettings[NetworkSettings.IP_KEY];
                ns = new NetworkSettings(p, ip);
            }
            return ns;
        }

        public ServiceSettings GetServiceSettings()
        {
            if (ss == null)
            {
                var cS = ConfigurationManager.AppSettings[ServiceSettings.COMMAND_SERVICE];
                var dS = ConfigurationManager.AppSettings[ServiceSettings.DEVICE_SERVICE];
                var aS = ConfigurationManager.AppSettings[ServiceSettings.ACCOUNT_SERVICE];
                var nS = ConfigurationManager.AppSettings[ServiceSettings.NOTIFICATION_SERVICE];
                ss = new ServiceSettings(cS, dS, aS, nS);
            }
            return ss;
        }
    }
}
