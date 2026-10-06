using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Enums;
using Tausend.RelayServer.Enums;

namespace Tausend.RelayServer.Commands
{
    public class StatusCommand : BaseCommand
    {
        protected override string CommandCode { get { return COMMANDS.STATUS; } }

        private StatusOptions _option;
        private int _partition;

        public StatusCommand() : this(StatusOptions.GENERAL_STATUS)
        {
        }

        public StatusCommand(StatusOptions option) : this(option, 0)
        {            
        }

        public StatusCommand(StatusOptions option, int partition) : base()
        {
            _option = option;
            _partition = partition;
        }

        protected override string GetCommand()
        {
            return CommandCode + _option.ToOptionChar();
        }
    }
}
