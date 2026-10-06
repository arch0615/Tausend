using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tausend.UDPListener.Enums
{
    public enum MessageType
    {
        UNKNOWN = 0x00,
        // Mensajes del cliente al servidor
        ID = 0x01, IDQ = 0x02, DATA = 0x03, CON = 0x04, EVENT = 0x05,
        // Mensajes del servidor al cliente
        IDOK = 0x81, IDFULL = 0x82, IDQOK = 0x83, IDQNE = 0x84,
        CONOK = 0x85, CONRJ = 0x86, EVENTOK = 0x87
    }
}
