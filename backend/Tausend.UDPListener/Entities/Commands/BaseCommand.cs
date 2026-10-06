using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.RelayServer.Commands
{
    public abstract class BaseCommand
    {
        public BaseCommand() { }

        protected abstract string GetCommand();

        protected abstract string CommandCode { get; }

        protected virtual bool IsValidCommand()
        {
            return true;
        }

        public string GetIpCommand()
        {
            return GetCommand();
            //var command = GetCommand();
            //return COMMANDS.IP_COMMAND_START_CHAR
            //       + command
            //       + COMMANDS.IP_COMMAND_END_CHAR;
        }

        public string GetSMSCommand(string pin)
        {
            var command = GetCommand();
            return COMMANDS.SMS_COMMAND_START_CHAR
                   + pin
                   + command
                   + COMMANDS.SMS_COMMAND_END_CHAR;
        }
    }
}
