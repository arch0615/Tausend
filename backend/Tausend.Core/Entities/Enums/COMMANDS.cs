using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tausend.Backend.Enums
{
    public static class COMMANDS
    {
        /// <summary>
        /// Consulta de versión del firmware
        /// </summary>
        public const string VERSION = "VER";

        /// <summary>
        /// Cambio o lectura de una sección de programación
        /// </summary>
        public const string PROGRAM_SECTION = "PRG";

        /// <summary>
        /// Control de Salidas PGM
        /// </summary>
        public const string PROGRAM_CONTROL = "PGM";

        /// <summary>
        /// Status
        /// </summary>
        public const string STATUS = "STS";

        /// <summary>
        /// Fail Status
        /// </summary>
        public const string FAIL_STATUS = "STSF";

        /// <summary>
        /// Zones Status
        /// </summary>
        public const string ZONES_STATUS = "STSZ";

        /// <summary>
        /// Memory Status
        /// </summary>
        public const string MEMORY_STATUS = "STSM";

        /// <summary>
        /// Programación del reloj
        /// </summary>
        public const string CLOCK = "RTC";

        /// <summary>
        /// Exclusión de zonas
        /// </summary>
        public const string BYPASS = "BYP";

        /// <summary>
        /// Armar
        /// </summary>
        public const string ARM = "ARM";

        /// <summary>
        /// Desarmar
        /// </summary>
        public const string DISARM = "DAR";

        /// <summary>
        /// Firmware Update
        /// </summary>
        public const string FIRMWARE_UPDATE = "FWU";

        /// <summary>
        /// Funciones Especiales
        /// </summary>
        public const string SPECIAL_FUNCTION = "FUN";

        /// <summary>
        /// Caracter de inicio de comando IP
        /// </summary>
        public const string IP_COMMAND_START_CHAR = "<";

        /// <summary>
        /// Caracter de fin de comando IP
        /// </summary>
        public const string IP_COMMAND_END_CHAR = ">";

        /// <summary>
        /// Caracter de inicio de comando SMS
        /// </summary>
        public const string SMS_COMMAND_START_CHAR = "*";

        /// <summary>
        /// Caracter de fin de comando SMS
        /// </summary>
        public const string SMS_COMMAND_END_CHAR = "*";


        /// <summary>
        /// Caracter de separador entre comando y parametros
        /// </summary>
        public const string PARAMS_CHAR = ":";

        /// <summary>
        /// Caracter de separador entre items de una lista de parametros
        /// </summary>
        public const string SEPARATOR_CHAR = ",";

        /// <summary>
        /// Comando de default
        /// </summary>
        public const string DEFAULT = "DEF";
    }
}
