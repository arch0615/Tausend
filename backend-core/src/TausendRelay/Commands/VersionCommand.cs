using TausendRelay.Enums;

namespace TausendRelay.Commands
{
    public class VersionCommand : BaseCommand
    {
        protected override string GetCommand() => CommandCodes.Version;
    }
}
