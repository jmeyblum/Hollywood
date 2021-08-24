using UnityEngine;
using Hollywood.Runtime;

namespace Hollywood.Example
{
	[Owns(typeof(ApplicationStateMachine))]
	public class Application
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Main()
		{
			Injector.GetInstance<Application>();
		}
	}
}