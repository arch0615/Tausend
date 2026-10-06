using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Tausend.RelayServer.Business;
using Tausend.RelayServer.Services;
using Tausend.UDPListener.Listener;

namespace Tausend.UDPListener
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(PrivateService));
            host.Open();
            Listener.UDPListener.StartServer();
            Console.ReadKey(true);
        }
    }
}
