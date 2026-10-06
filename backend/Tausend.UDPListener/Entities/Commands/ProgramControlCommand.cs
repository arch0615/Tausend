using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.RelayServer.Commands
{
    public class ProgramControlCommand : BaseCommand
    {
        protected override string CommandCode { get { return COMMANDS.PROGRAM_CONTROL; } }

        private int _zone;
        private bool _state;

        public ProgramControlCommand(int zone) : this(zone, true)
        {
        }

        public ProgramControlCommand(int zone, bool state) : base()
        {
            _zone = zone;
            _state = state;
        }

        protected override string GetCommand()
        {
            return CommandCode + _zone + COMMANDS.PARAMS_CHAR + (_state ? "1" : "0");
        }
    }
}
