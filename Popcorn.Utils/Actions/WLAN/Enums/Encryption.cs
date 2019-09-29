using System.ComponentModel;

namespace Popcorn.Utils.Actions.WLAN.Enums
{
    public enum Encryption
    {
        [Description("none")]
        None,
        [Description("WEP")]
        Wep,
        [Description("TKIP")]
        Tkip,
        [Description("AES")]
        Aes,
    }
}
