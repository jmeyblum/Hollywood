using Hollywood.Runtime;
using Hollywood.Runtime.StateMachine;
using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Example
{
	public class SessionBootState : IState, IInitializable
	{
		[Needs]
		private SessionStateMachine _sessionStateMachine;

		Task IInitializable.Initialize(CancellationToken token)
		{
			_sessionStateMachine.TransitionTo<SessionLoginState>();

			return Task.CompletedTask;
		}
	}
}