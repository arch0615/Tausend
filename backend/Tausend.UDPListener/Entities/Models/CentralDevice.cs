using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.UDPListener.Entities
{
    public class CentralDevice
    {
        public int ID { get; set; }
        public string Identifier { get; set; }
        public IPAddress IP { get; set; }
        public int Port { get; set; }
        public int LastMessageId { get; set; }
        public ushort PublicKey { get; set; }
    }
}
