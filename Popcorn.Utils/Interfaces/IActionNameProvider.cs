using System;

namespace Popcorn.Utils.Interfaces
{
    internal interface IActionNameProvider
	{
		/// <summary>
		/// Gets the text to output to the netsh command
		/// </summary>
		string ActionName { get; }
	}
}