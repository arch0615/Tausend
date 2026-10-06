using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.Backend.Commands
{
    public class VersionCommand : BaseCommand
    {
        public VersionCommand() : base()
        {
        }

        protected override string CommandCode { get { return COMMANDS.VERSION; } }

        protected override string getCommand()
        {
            return CommandCode;
        }
    }
}
