using System.ComponentModel;

namespace Popcorn.Utils.Actions.WLAN.Enums
{
    public enum AuthMode
    {
        [Description("machineOrUser")]
        MachineOrUser,
        [Description("machineOnly")]
        MachineOnly,
        [Description("userOnly")]
        UserOnly,
        [Description("guest")]
        Guest
    }
}
