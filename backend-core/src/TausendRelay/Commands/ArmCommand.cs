using TausendRelay.Enums;

namespace TausendRelay.Commands
{
    public class ArmCommand : BaseCommand
    {
        private readonly ArmMode _mode;
        private readonly int _partition;
        private readonly string _pin;

        public ArmCommand(ArmMode mode, string pin) : this(mode, -1, pin)
        {
        }

        public ArmCommand(ArmMode mode, int partition, string pin)
        {
            _mode = mode;
            _partition = partition;
            _pin = pin;
        }

        protected override string GetCommand()
        {
            var command = CommandCodes.Arm + _mode.ToArmModeChar();
            if (_partition >= 0)
                command += _partition;
            command += CommandCodes.ParamsChar + _pin;
            return command;
        }
    }
}
