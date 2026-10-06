using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.Backend.Commands
{
    public abstract class BaseCommand
    {
        public BaseCommand() { }

        protected abstract string getCommand();

        protected abstract string CommandCode { get; }

        public string getIpCommand()
        {
            var command = getCommand();
            return COMMANDS.IP_COMMAND_START_CHAR
                   + command
                   + COMMANDS.IP_COMMAND_END_CHAR;
        }

        public string getSMSCommand(string pin)
        {
            var command = getCommand();
            return COMMANDS.SMS_COMMAND_START_CHAR
                   + pin
                   + command
                   + COMMANDS.SMS_COMMAND_END_CHAR;
        }
    }
}
