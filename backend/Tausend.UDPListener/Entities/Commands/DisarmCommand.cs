using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.RelayServer.Commands
{
    public class DisarmCommand : BaseCommand
    {
        protected override string CommandCode { get { return COMMANDS.DISARM; } }

        private int _partition;
        private string _pin;

        public DisarmCommand(int partition, string pin) : base()
        {
            _partition = partition;
            _pin = pin;
        }

        public DisarmCommand(string pin) : this(-1, pin)
        {
        }

        protected override string GetCommand()
        {
            string command = CommandCode;
            if (_partition >= 0)
            {
                command += _partition.ToString();
            }
            command += COMMANDS.PARAMS_CHAR + _pin;
            return command;
        }
    }
}
