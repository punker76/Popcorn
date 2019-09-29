using System;
using Popcorn.Utils.Utilities;

namespace Popcorn.Utils.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
	internal class ParameterNameAttribute : Attribute
	{
		public ParameterNameAttribute(string parameterName) {
			ParameterName = parameterName;
		}

		public ParameterNameAttribute(string parameterName, BooleanType booleanType)
		{
			ParameterName = parameterName;
			BooleanType = booleanType;
		}

		public String ParameterName { get; }
		public BooleanType BooleanType { get; }
	}
}