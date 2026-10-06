namespace TausendRelay.Enums
{
    public enum StatusOptions
    {
        Zone,
        Exclusion,
        Memory,
        Fail,
        GeneralStatus,
        Communicator,
        Battery,
    }

    public static class StatusOptionsExtensions
    {
        public static string ToOptionChar(this StatusOptions status) => status switch
        {
            StatusOptions.Zone => "Z",
            StatusOptions.Exclusion => "X",
            StatusOptions.Memory => "M",
            StatusOptions.Fail => "F",
            StatusOptions.GeneralStatus => "A",
            StatusOptions.Communicator => "C",
            StatusOptions.Battery => "B",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid option for status"),
        };
    }
}
