using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.RelayServer.Commands
{
    public class ClockCommand : BaseCommand
    {
        protected override string CommandCode { get { return COMMANDS.CLOCK; } }

        private int _hour;
        private int _minute;
        private int _second;
        private int _day;
        private int _month;
        private int _year;

        public ClockCommand() : this(-1, -1)
        {            
        }

        public ClockCommand(int hour, int minute) : this(hour, minute, -1)
        {
        }

        public ClockCommand(int hour, int minute, int second) : this(hour, minute, second, 1, -1, -1)
        {
        }

        public ClockCommand(int hour, int minute, int day, int month, int year) : this(hour, minute, -1, day, month, year)
        {
        }

        public ClockCommand(int hour, int minute, int second, int day, int month, int year) : base()
        {
            _hour = hour;
            _minute = minute;
            _second = second;
            _day = day;
            _month = month;
            _year = year;
        }


        protected override string GetCommand()
        {
            var command = CommandCode;
            if (IsValidParam(_hour) && IsValidParam(_minute))
            {
                command += COMMANDS.PARAMS_CHAR + ParamToString(_hour) + ParamToString(_minute);
                if (IsValidParam(_second))
                {
                    command += ParamToString(_second);
                }
                if (IsValidParam(_day) && IsValidParam(_month) && IsValidParam(_year))
                {
                    command += COMMANDS.SEPARATOR_CHAR + ParamToString(_day) + ParamToString(_month) + ParamToString(_year);
                }
            }
            return command;
        }

        private bool IsValidParam(int param)
        {
            return param >= 0;
        }

        private string ParamToString(int param)
        {
            return param.ToString("00");
        }
    }
}
