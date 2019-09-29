namespace Popcorn.Utils.Interfaces
{
    internal interface IInitializable
	{
		void Initialize(string priorText, IExecutionHarness harness);
	}
}