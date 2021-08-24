using Hollywood.Runtime;
using Hollywood.Runtime.StateMachine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hollywood.Example
{
	[Owns(typeof(Game))]
	public class SessionGameState : IState
	{
	}
}