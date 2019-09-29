using System.ComponentModel;

namespace Popcorn.Utils.Actions.WLAN.Enums
{
    public enum Cost
    {
        [Description("default")]
        Default,
        [Description("unrestricted")]
        Unrestricted,
        [Description("fixed")]
        Fixed,
        [Description("variable")]
        Variable,

    }
}
