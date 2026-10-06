using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting
{
    [TestClass]
    public class UDPTest
    {
        [TestMethod]
        public void TestIDQReq()
        {
            Socket listener;
            int localPort = 10000;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (int)1);
            listener.Bind(localEndPoint);
            //la ip es 18.219.225.37 y el puerto 10000
            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Parse("18.219.225.37"), 10000);
            //CNC16518002E0001
            string txt = "(0001,0000)02<CNC16518002E0001>";
            byte[] bytes = Encoding.ASCII.GetBytes(txt);
            listener.SendTo(bytes, bytes.Length, SocketFlags.None, remoteEndpoint);
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            byte[] bytesrec = new byte[512];
            var bytesRec = listener.ReceiveFrom(bytesrec, ref remote);
            var data = Encoding.ASCII.GetString(bytesrec, 0, bytesrec.Length);
            Console.WriteLine(data);
        }
    }
}
