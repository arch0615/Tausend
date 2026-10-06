using TausendRelay.Enums;

namespace TausendRelay.Commands
{
    public class ProgramControlCommand : BaseCommand
    {
        private readonly int _zone;
        private readonly bool _state;

        public ProgramControlCommand(int zone, bool state)
        {
            _zone = zone;
            _state = state;
        }

        protected override string GetCommand() =>
            CommandCodes.ProgramControl + _zone + CommandCodes.ParamsChar + (_state ? "1" : "0");
    }
}
