using TausendRelay.Enums;

namespace TausendRelay.Commands
{
    /// <summary>Zone bypass ("exclusion") command -- BYPCommand's toggle path
    /// (Listener/UdpRelayService.SendBypCommand) always constructs this with a zone list, never
    /// the parameterless "get" form, matching the original's only actual call site.</summary>
    public class ExclusionCommand : BaseCommand
    {
        private readonly List<int> _zones;

        public ExclusionCommand(List<int> zones)
        {
            _zones = zones;
        }

        protected override string GetCommand()
        {
            var command = CommandCodes.Bypass + CommandCodes.ParamsChar;
            if (_zones.Count == 0)
                return command;
            return command + string.Join(CommandCodes.SeparatorChar, _zones.Select(z => z.ToString("00")));
        }
    }
}
