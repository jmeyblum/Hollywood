using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Runtime.StateMachine
{
	public class StateMachine<TInitialState> : IInitializable where TInitialState : class, IState
	{
		public IState State { get; private set; }

		Task IInitializable.Initialize(CancellationToken token)
		{
			State = Injector.GetInstance<TInitialState>(this);

			return Task.CompletedTask;
		}

		public void TransitionTo<TState>() where TState : class, IState
		{
			Injector.DisposeInstance(State);

			State = Injector.GetInstance<TState>(this);
		}
	}
}