using TausendRelay.Enums;

namespace TausendRelay.Commands
{
    public class StatusCommand : BaseCommand
    {
        private readonly StatusOptions _option;

        public StatusCommand() : this(StatusOptions.GeneralStatus)
        {
        }

        public StatusCommand(StatusOptions option)
        {
            _option = option;
        }

        protected override string GetCommand() => CommandCodes.Status + _option.ToOptionChar();
    }
}
