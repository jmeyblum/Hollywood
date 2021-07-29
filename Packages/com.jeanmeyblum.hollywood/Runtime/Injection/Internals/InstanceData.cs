using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Runtime.Internal
{
	public class InstanceData
	{
		public InstanceState State = InstanceState.UnResolved;

		public HashSet<object> ResolvingNeeds;

		public Task ResolvingTask;
		public Task InitializationTask;

		public CancellationTokenSource TaskTokenSource;
	}
}