using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Enums;
using Tausend.RelayServer.Enums;

namespace Tausend.RelayServer.Commands
{
    public class ArmCommand : BaseCommand
    {
        protected override string CommandCode { get { return COMMANDS.ARM; } }

        private ArmMode _mode;
        private int _partition;
        private string _pin;

        public ArmCommand(ArmMode mode, int partition, string pin) : base()
        {
            _mode = mode;
            _partition = partition;
            _pin = pin;
        }

        public ArmCommand(ArmMode mode, string pin) : this(mode, -1, pin)
        {
        }

        protected override string GetCommand()
        {
            var command = CommandCode + _mode.ToArmModeChar();
            if (_partition >= 0)
            {
                command += _partition.ToString();
            }
            command += COMMANDS.PARAMS_CHAR + _pin;
            return command;
        }
    }
}
