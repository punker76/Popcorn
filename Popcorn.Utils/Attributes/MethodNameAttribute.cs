using System;

namespace Popcorn.Utils.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
	internal class MethodNameAttribute : Attribute
	{
		public MethodNameAttribute(string methodName)
		{ MethodName = methodName; }

		public String MethodName { get; }
	}
}