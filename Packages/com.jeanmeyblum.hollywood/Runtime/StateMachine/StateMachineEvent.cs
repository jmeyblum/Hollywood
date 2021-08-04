using System;

namespace Hollywood.Runtime.StateMachine
{
	public abstract class StateMachineEvent
	{
		public abstract StateMachineEventType Type { get; }
	}

	public class StateMachineEventPreEnterState : StateMachineEvent
	{
		public override StateMachineEventType Type => StateMachineEventType.OnPreEnterState;

		public readonly IState PreviousState;
		public readonly Type NextStateType;

		public StateMachineEventPreEnterState(IState previousState, Type newStateType)
		{
			PreviousState = previousState;
			NextStateType = newStateType;
		}
	}

	public class StateMachineEventPostEnterState : StateMachineEvent
	{
		public override StateMachineEventType Type => StateMachineEventType.OnPostEnterState;

		public readonly IState PreviousState;
		public readonly IState CurrentState;

		public StateMachineEventPostEnterState(IState previousState, IState currentState)
		{
			PreviousState = previousState;
			CurrentState = currentState;
		}
	}

	public class StateMachineEventPreExitState : StateMachineEvent
	{
		public override StateMachineEventType Type => StateMachineEventType.OnPreExitState;

		public readonly IState CurrentState;
		public readonly Type NextStateType;

		public StateMachineEventPreExitState(IState currentState, Type nextStateType)
		{
			CurrentState = currentState;
			NextStateType = nextStateType;
		}
	}

	public class StateMachineEventPostExitState : StateMachineEvent
	{
		public override StateMachineEventType Type => StateMachineEventType.OnPostExitState;

		public readonly IState PreviousState;
		public readonly Type NextStateType;

		public StateMachineEventPostExitState(IState previousState, Type nextStateType)
		{
			PreviousState = previousState;
			NextStateType = nextStateType;
		}
	}
}