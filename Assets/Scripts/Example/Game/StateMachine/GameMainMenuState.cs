using Hollywood.StateMachine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hollywood.Example
{
	public class GameMainMenuState : IState, IUpdatable, IInitializable
	{
		[Needs]
		private GameStateMachine _gameStateMachine;

		[Needs]
		private SessionStateMachine _sessionStateMachine;

		[Needs]
		private ApplicationStateMachine _applicationStateMachine;

		Task IInitializable.Initialize(CancellationToken token)
		{
			Debug.Log("Main Menu");

			return Task.CompletedTask;
		}

		async Task IUpdatable.Update(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				if (Input.GetKeyDown(KeyCode.R))
				{
					_gameStateMachine.TransitionTo<GameBootState>();
				}
				else if (Input.GetKeyDown(KeyCode.O))
				{
					_sessionStateMachine.TransitionTo<SessionBootState>();
				}
				else if (Input.GetKeyDown(KeyCode.F))
				{
					_applicationStateMachine.TransitionTo<ApplicationBootState>();
				}

				await Task.Yield();
			}
		}
	}
}
