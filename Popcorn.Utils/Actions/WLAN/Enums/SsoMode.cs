using System.ComponentModel;

namespace Popcorn.Utils.Actions.WLAN.Enums
{
    public enum SsoMode
    {
        [Description("preLogon")]
        PreLogon,
        [Description("postLogon")]
        PostLogon,
        [Description("none")]
        None
    }
}
