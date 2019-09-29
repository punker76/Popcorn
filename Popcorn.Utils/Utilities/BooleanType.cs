using Popcorn.Utils.Attributes;

namespace Popcorn.Utils.Utilities
{
    public enum BooleanType
	{
		[BooleanValue("yes", "no")]
		YesNo,
		[BooleanValue("enable", "disable")]
		EnableDisable,
		[BooleanValue("true","false")]
		TrueFalse
	}
}