namespace TausendRelay.Commands
{
    public abstract class BaseCommand
    {
        protected abstract string GetCommand();

        public string GetIpCommand() => GetCommand();
    }
}
