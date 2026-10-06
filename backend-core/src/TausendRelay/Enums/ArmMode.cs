namespace TausendRelay.Enums
{
    public enum ArmMode
    {
        Stay,
        Away,
        Night,
    }

    public static class ArmModeExtensions
    {
        public static string ToArmModeChar(this ArmMode mode) => mode switch
        {
            ArmMode.Stay => "S",
            ArmMode.Away => "A",
            ArmMode.Night => "N",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid ArmMode"),
        };
    }
}
