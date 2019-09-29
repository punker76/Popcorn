using System.Collections.Generic;
using Popcorn.Utils.ResponseProcessors;

namespace Popcorn.Utils.Interfaces
{
    internal interface IResponseProcessor
	{
		StandardResponse ProcessResponse(IEnumerable<string> responseLines, int exitCode, string splitRegEx = null);
	}
}