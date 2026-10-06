using TausendRelay.Enums;

namespace TausendRelay.Commands
{
    public class DisarmCommand : BaseCommand
    {
        private readonly int _partition;
        private readonly string _pin;

        public DisarmCommand(string pin) : this(-1, pin)
        {
        }

        public DisarmCommand(int partition, string pin)
        {
            _partition = partition;
            _pin = pin;
        }

        protected override string GetCommand()
        {
            var command = CommandCodes.Disarm;
            if (_partition >= 0)
                command += _partition;
            command += CommandCodes.ParamsChar + _pin;
            return command;
        }
    }
}
