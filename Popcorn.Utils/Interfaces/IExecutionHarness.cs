using System;
using System.Collections.Generic;

namespace Popcorn.Utils.Interfaces
{
    public interface IExecutionHarness
	{
		IEnumerable<String> Execute(string action, out int exitCode);
	}
}