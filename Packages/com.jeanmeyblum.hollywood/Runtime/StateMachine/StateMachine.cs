using Hollywood.Observer;
using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.StateMachine
{
	/// <summary>
	/// A flat state machine that can transitioned from state to state.
	/// When transitioning to a new state the old state gets disposed as well its owned systems
	/// and the new state get injected and is owned by the state machine.
	/// </summary>
	/// <typeparam name="TInitialState"></typeparam>
	[Owns(typeof(ObservableHandler<StateMachineEvent>))]
	public class StateMachine<TInitialState> : IInitializable, IObservable<StateMachineEvent> where TInitialState : class, IState
	{
		public IState State { get; private set; }

		[Needs]
		private ObservableHandler<StateMachineEvent> ObservableHandler;

		public virtual Task Initialize(CancellationToken token)
		{
			ObservableHandler.Send(new StateMachineEventPreEnterState(null, typeof(TInitialState)));

			State = Injector.GetInstance<TInitialState>(this);

			ObservableHandler.Send(new StateMachineEventPostEnterState(null, State));

			return Task.CompletedTask;
		}

		public void TransitionTo<TState>() where TState : class, IState
		{
			ObservableHandler.Send(new StateMachineEventPreExitState(State, typeof(TState)));

			Injector.DisposeInstance(State);

			var previousState = State;
			State = null;

			ObservableHandler.Send(new StateMachineEventPostExitState(previousState, typeof(TState)));

			ObservableHandler.Send(new StateMachineEventPreEnterState(previousState, typeof(TState)));

			State = Injector.GetInstance<TState>(this);

			ObservableHandler.Send(new StateMachineEventPostEnterState(previousState, State));
		}

		public IUnsubscriber Subscribe(IObserver<StateMachineEvent> observer)
		{
			return ObservableHandler.Subscribe(observer);
		}
	}
}