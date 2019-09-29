using System.Collections.Generic;
using System.Linq;
using Popcorn.Utils.Interfaces;

namespace Popcorn.Utils.ResponseProcessors
{
    internal class SkipHeaderProcessor : IResponseProcessor
	{
		StandardResponse IResponseProcessor.ProcessResponse(IEnumerable<string> responseLines, int exitCode, string splitRegEx)
		{
			IResponseProcessor standardResponse = new StandardResponse();
			standardResponse.ProcessResponse(responseLines.Skip(3), exitCode);
			return (StandardResponse) standardResponse;
		}
	}
}