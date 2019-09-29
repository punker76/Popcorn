using Popcorn.Utils.Actions.HTTP;
using Popcorn.Utils.Actions.WLAN;

namespace Popcorn.Utils.Actions
{
    public interface INetSH
	{
		/// <summary>
		/// Represents an HTTP action (currently the only actions available in NetSH are HTTP).
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/cc307236(v=vs.85).aspx">MSDN</a>.
		/// </summary>
		IHttpAction Http { get; }

        IWlanAction Wlan { get; }
	}
}