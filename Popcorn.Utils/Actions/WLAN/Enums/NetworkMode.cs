using System.ComponentModel;

namespace Popcorn.Utils.Actions.WLAN.Enums
{
    public enum NetworkMode
    {
        [Description("ssid")]
        Ssid,
        [Description("bssid")]
        Bssid
    }
}