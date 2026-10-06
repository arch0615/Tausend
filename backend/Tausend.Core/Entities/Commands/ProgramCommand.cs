using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tausend.Backend.Enums;

namespace Tausend.Backend.Commands
{
    public class ProgramCommand : BaseCommand
    {
        enum ParamType { NONE, DEFAULT, STRING, INT, BOOLEAN }
        protected override string CommandCode { get { return COMMANDS.PROGRAM_SECTION; } }

        private string _stringParam;
        private List<int> _intParams;
        private List<short> _booleanParams;
        private int _section;
        private ParamType _paramType;

        public ProgramCommand(int section, string param) :base()
        {
            _stringParam = param;
            _section = section;
            _paramType = ParamType.STRING;
        }

        public ProgramCommand(int section, List<int> intParams) : base()
        {
            _intParams = intParams;
            _section = section;
            _paramType = ParamType.INT;
        }

        public ProgramCommand(int section, List<short> booleansParams) : base()
        {
            _booleanParams = booleansParams;
            _section = section;
            _paramType = ParamType.BOOLEAN;
        }

        public ProgramCommand(int section, bool def)
        {
            _section = section;
            _paramType = def ? ParamType.DEFAULT : ParamType.NONE;
        }

        protected override string getCommand()
        {
            return CommandCode + _section + getParams();
        }

        private string intParamsToString()
        {
            return COMMANDS.PARAMS_CHAR + string.Join(COMMANDS.SEPARATOR_CHAR, _intParams);
        }

        private string booleanParamsToString()
        {
            return COMMANDS.PARAMS_CHAR + string.Join(COMMANDS.SEPARATOR_CHAR, _booleanParams);
        }

        private string stringParams()
        {
            return COMMANDS.PARAMS_CHAR + _stringParam;
        }

        private string getParams()
        {
            switch (_paramType)
            {
                case ParamType.NONE:
                    return "";
                case ParamType.DEFAULT:
                    return COMMANDS.DEFAULT;
                case ParamType.STRING:
                    return stringParams();
                case ParamType.INT:
                    return intParamsToString();
                case ParamType.BOOLEAN:
                    return booleanParamsToString();
                default:
                    return "";
            }
        }
    }
}
