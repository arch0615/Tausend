using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GAMLib
{
    class CGM1
    {
        private Random rand;
        private UInt16 psr;
        private UInt16 CGM1_PSR_POLY = 0xb400;   // polinomio generador del pseudorandom
        private UInt32 CGM1_SEEDC_PRIME = 41243; // numero primo para semilla tipo C

        public CGM1()
        {
            rand = new Random();
        }

        // Genera el proximo valor de la secuencia pseudoaleatria
        void nextPsr()
        {
            int lsb;

            lsb = psr & 1;
            psr >>= 1;
            if (lsb != 0)
                psr ^= CGM1_PSR_POLY;
        }

        // Encripta o desencripta (es el mimso proceso)
        private void encriptDecript(byte[] src, int start, int len, UInt16 seed)
        {
            int i;
            byte x;

            // Hace la XOR con la parte alta y la baja del generador pseudorandom
            for (i = 0, psr = seed; i < len; i++)
            {
                x = (byte)(psr & 0xff);       // Aca se puede tomar otros bits del psr o cominarlos
                x ^= src[i + start];
                src[i + start] = x;
                nextPsr();
            }
        }

        // Genera semilla con algoritmo B
        private UInt16 seedGenB(UInt16 kS, UInt16 kV)
        {
            UInt32 x;

            if (kV < 0x1000)
                kV ^= 0xffff;

            x = (UInt32)kS * (UInt32)kV;
            x += (UInt32)kS + (UInt32)kV + (UInt32)(kV >> 8);

            x &= 0xffff;

            return (UInt16)x;
        }

        // Genera semilla con algoritmo C
        private UInt16 seedGenC(UInt16 kS, UInt16 kV)
        {
            UInt32 x;

            x = (UInt32)kS + kV + (UInt32)(kV >> 8);
            x *= CGM1_SEEDC_PRIME;

            x &= 0xffff;

            return (UInt16)x;
        }

        // Genera la semilla para la ecriptacion en funcion de las calves secreta y variable
        private UInt16 seedGen(UInt16 kS, UInt16 kV)
        {
            UInt16 seed;

            seed = (UInt16)(seedGenB(kS, kV) ^ seedGenC(kS, kV));

            return seed;
        }

        // Genera la clave variable
        private UInt16 keyVarGen()
        {
            byte[] b = new byte[2];
            UInt16 x;

            rand.NextBytes(b);
            x = b[1];
            x <<= 8;
            x |= b[0];

            return x;
        }

        private byte chksum(byte[] src, int len)
        {
            int i;
            byte sum;

            for (i = 0, sum = 0; i < len; i++)
                sum += src[i];

            return sum;
        }

        // Funcion de encriptacion, version para enciptar binario
        public string encript(byte[] src, UInt16 kS)
        {
            int len = src.Length;
            UInt16 kV;
            UInt16 seed;
            byte[] buffer = new byte[len + 3];
            int i;

            // Genera la clave variable
            kV = keyVarGen();

            // Genera la semilla
            seed = seedGen(kS, kV);

            // Pone la clave variable	
            buffer[0] = (byte)(kV & 0xff);
            buffer[1] = (byte)((kV >> 8) & 0xff);

            // Copia los datos
            for (i = 0; i < len; i++)
                buffer[i + 2] = src[i];

            // Calcula el checksum y lo agrega (el checksum incluye la clave variable)
            buffer[len + 2] = chksum(buffer, len + 2);

            // Encripta todo menos la clave variable, incluido el checksum
            encriptDecript(buffer, 2, len + 1, seed);

            return (System.Convert.ToBase64String(buffer, 0, len + 3));
        }

        // Funcion de desencriptacion, version para desenciptar binario
        public byte[] decript(string srcStr, UInt16 kS)
        {
            UInt16 kV;
            UInt16 seed;
            int i;

            // Decodifica base64, debe venir el menos la clave, el checksum y un dato
            byte[] buffer = Convert.FromBase64String(srcStr);
            int len = buffer.Length;
            if (len < 4)
                return new byte[0];

            // Extrae la clave variable
            kV = buffer[1];
            kV <<= 8;
            kV |= buffer[0];

            // Genera la semilla
            seed = seedGen(kS, kV);

            // Desencripta todo menos la clave variable, incluido el checksum
            encriptDecript(buffer, 2, len - 2, seed);

            // Verifica el checksum
            if (buffer[len - 1] != chksum(buffer, len - 1))
                return new byte[0];

            // Copia los datos al destino
            byte[] dest = new byte[len - 3];
            for (i = 0; i < len - 3; i++)
                dest[i] = buffer[i + 2];

            return dest;
        }

        // Funcion de encriptacion, version para enciptar texto
        public string encript(string src, UInt16 key)
        {
            byte[] bin = ASCIIEncoding.ASCII.GetBytes(src);
            return encript(bin, key);
        }

        // Funcion de desencriptacion, version para desenciptar texto
        public string decriptStr(string srcStr, UInt16 kS)
        {
            byte[] deco = decript(srcStr, kS);

            return System.Text.Encoding.Default.GetString(deco);
        }
    }
}
