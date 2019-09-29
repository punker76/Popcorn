using System.ComponentModel;

namespace Popcorn.Utils.Actions.WLAN.Enums
{
    public enum Permission
    {
        [Description("allow")]
        Allow,
        [Description("block")]
        Block,
        [Description("denyall")]
        DenyAll
    }
}
