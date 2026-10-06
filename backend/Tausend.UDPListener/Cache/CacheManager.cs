using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Models;
using Tausend.RelayServer.Business;
using Tausend.RelayServer.Settings;
using Tausend.UDPListener.Entities;

namespace Tausend.UDPListener.Cache
{
    public static class CacheManager
    {
        private static List<CentralDevice> devices;
        private static int nextId;

        public static void LoadCache()
        {
            devices = new List<CentralDevice>();
            nextId = 1;
        }

        public static CentralDevice AddCentralDevice(string identifier, IPAddress ip, int port, long source)
        {
            CentralDevice device = null;
            //if (source > 0) nextId = source + 1;
            if (nextId < 10000)
            {
                device = new CentralDevice()
                {
                    ID = nextId,
                    Identifier = identifier,
                    IP = ip,
                    Port = port
                };
                var dbDev = GetDeviceByIdentifier(identifier);
                if(dbDev != null)
                    device.PublicKey = dbDev.PublicKey;
                devices.Add(device);
                nextId++;
            }
            return device;
        }

        public static CentralDevice GetCentralDeviceByID(long id)
        {
            var device = devices.Find(x => x.ID == id);
            //if (device == null)
            //    device = GetDevice(id);
            return device;
        }

        public static CentralDevice GetCentralDeviceByIdentifier(string identifier)
        {
            var device = devices.Find(x => x.Identifier.Equals(identifier));
            //if (device == null)
            //    device = GetDevice(identifier);
            return device;
        }

        public static void RefreshCentralDeviceData(long id, IPAddress ip, int port)
        {
            var device = GetCentralDeviceByID(id);
            device.IP = ip;
            device.Port = port;
        }

        private static CentralDevice GetDeviceByIdentifier(string identifier)
        {
            ServiceBusiness bz = new ServiceBusiness();
            var device = bz.GetDevice(identifier);
            CentralDevice res = DeviceToCentral(device);
            return res;
        }

        private static CentralDevice GetDeviceById(long id)
        {
            ServiceBusiness bz = new ServiceBusiness();
            var device = bz.GetDevice(id);
            CentralDevice res = DeviceToCentral(device);
            return res;
        }

        private static CentralDevice DeviceToCentral(Device device)
        {
            if (device == null) return null;
            CentralDevice res = new CentralDevice
            {
                ID = nextId,
                Identifier = device.Identifier,
                PublicKey = device.PublicKey
                //Port = int.Parse(device.Port),
                //IP = IPAddress.Parse(device.IP)
            };
            nextId++;
            //if (res.ID > nextId)
            //    nextId = res.ID + 1;
            return res;
        }

        public static bool IsNewMessage(long id, int messageId)
        {
            var central = GetCentralDeviceByID(id);
            if (messageId == 0 || central.LastMessageId < messageId)
            {
                central.LastMessageId = messageId;
                return true;
            }
            return false;
        }
    }
}
