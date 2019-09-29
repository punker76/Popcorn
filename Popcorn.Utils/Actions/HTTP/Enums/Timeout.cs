using System.ComponentModel;

namespace Popcorn.Utils.Actions.HTTP.Enums
{
	public enum Timeout
	{
		[Description("idleconnectiontimeout")]
		IdleConnectionTimeout,
		[Description("headerwaittimeout")]
		HeaderWaitTimeout
	}
}