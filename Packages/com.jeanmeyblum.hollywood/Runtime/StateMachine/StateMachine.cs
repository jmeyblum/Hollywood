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
		private ObservableHandler<StateMachineEvent> _observableHandler;

		public virtual Task Initialize(CancellationToken token)
		{
			_observableHandler.Send(new StateMachineEventPreEnterState(null, typeof(TInitialState)));

			State = Injector.GetInstance<TInitialState>(this);

			_observableHandler.Send(new StateMachineEventPostEnterState(null, State));

			return Task.CompletedTask;
		}

		public void TransitionTo<TState>() where TState : class, IState
		{
			_observableHandler.Send(new StateMachineEventPreExitState(State, typeof(TState)));

			Injector.DisposeInstance(State);

			var previousState = State;
			State = null;

			_observableHandler.Send(new StateMachineEventPostExitState(previousState, typeof(TState)));

			_observableHandler.Send(new StateMachineEventPreEnterState(previousState, typeof(TState)));

			State = Injector.GetInstance<TState>(this);

			_observableHandler.Send(new StateMachineEventPostEnterState(previousState, State));
		}

		public IUnsubscriber Subscribe(IObserver<StateMachineEvent> observer)
		{
			return _observableHandler.Subscribe(observer);
		}
	}
}