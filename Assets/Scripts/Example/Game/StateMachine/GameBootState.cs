using Hollywood.StateMachine;
using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Example
{
	public class GameBootState : IState, IInitializable
	{
		[Needs]
		private GameStateMachine _gameStateMachine;

		Task IInitializable.Initialize(CancellationToken token)
		{
			_gameStateMachine.TransitionTo<GameMainMenuState>();

			return Task.CompletedTask;
		}
	}
}