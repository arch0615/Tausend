namespace TausendBackend.Api.Models
{
    // One AccountId<->DeviceId pair -- mirrors a distinct row of AccountDevicePins, minus the
    // PIN column (see EnumAccountDeviceLinks.sql for why it's excluded).
    public class AccountDeviceLink
    {
        public long AccountId { get; set; }
        public long DeviceId { get; set; }
    }
}
