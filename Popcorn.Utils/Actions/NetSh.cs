using Popcorn.Utils.Actions.HTTP;
using Popcorn.Utils.Actions.WLAN;
using Popcorn.Utils.Interfaces;

namespace Popcorn.Utils.Actions
{
    public sealed class NetSH : INetSH
	{
		private readonly IExecutionHarness _harness;

		public NetSH(IExecutionHarness harness)
		{
			_harness = harness;
		}

		/// <summary>
		/// Instantiates a new instance of NetSH with a CommandLineHarness
		/// </summary>
		public static NetSH CMD => new NetSH(new CommandLineHarness());

	    public IHttpAction Http => HttpAction.CreateAction("netsh", _harness);

	    public IWlanAction Wlan => WlanAction.CreateAction("netsh", _harness);
	}
}