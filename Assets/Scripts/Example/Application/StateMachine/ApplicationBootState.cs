using Hollywood.StateMachine;
using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Example
{
	public class ApplicationBootState : IState, IInitializable
	{
		[Needs]
		private ApplicationStateMachine _applicationStateMachine;

		Task IInitializable.Initialize(CancellationToken token)
		{
			_applicationStateMachine.TransitionTo<ApplicationSessionState>();

			return Task.CompletedTask;
		}
	}
}