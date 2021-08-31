using Hollywood.StateMachine;
using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Example
{
	public class SessionLoginState : IState, IUpdatable
	{
		[Needs]
		private LoginController _loginController;

		[Needs]
		private SessionStateMachine _sessionStateMachine;

		async Task IUpdatable.Update(CancellationToken token)
		{
			await _loginController.Login();

			token.ThrowIfCancellationRequested();

			_sessionStateMachine.TransitionTo<SessionGameState>();
		}
	}
}