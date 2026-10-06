using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.RelayServer.Commands
{
    public class ExclusionCommand : BaseCommand
    {
        protected override string CommandCode { get { return COMMANDS.BYPASS; } }
        private bool get = false;

        private List<int> _zones;

        public ExclusionCommand(List<int> zones) : base()
        {
            _zones = zones;
        }

        public ExclusionCommand() : this(null)
        {
            get = true;
        }

        protected override string GetCommand()
        {
            string command = CommandCode;
            if (!get)
            {
                command = command + COMMANDS.PARAMS_CHAR;
                command += GetZones();
            }
            return command;
        }

        private string GetZones()
        {
            if (_zones == null || _zones.Count == 0) return "";
            var strings = _zones.Select(s => s.ToString("00")).ToList();
            return string.Join(COMMANDS.SEPARATOR_CHAR, strings);
        }
    }
}
