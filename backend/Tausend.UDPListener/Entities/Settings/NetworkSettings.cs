using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.RelayServer.Settings
{
    public class NetworkSettings
    {
        public const string PORT_KEY = "puerto";
        public const string IP_KEY = "IP";
        public int Port { get; private set; }
        public string IP { get; private set; }

        public NetworkSettings(int port, string ip)
        {
            Port = port;
            IP = ip;
        }
    }
}
