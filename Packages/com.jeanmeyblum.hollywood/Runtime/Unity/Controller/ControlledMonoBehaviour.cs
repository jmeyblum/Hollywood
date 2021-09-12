using Hollywood.Controller;
using UnityEngine;

namespace Hollywood.Unity
{
	[ObservedMonoBehaviour]
	public abstract class ControlledMonoBehaviour<T, U> : MonoBehaviour, IControlledItem<T, U> where T : class, IItemController<U, T> where U : class, IControlledItem<T, U>
	{
		protected virtual void Awake()
		{

		}

		protected virtual void OnDestroy()
		{

		}

		protected T Controller { get; private set; }

		void IControlledItem<T>.OnControllerReceived(T controller)
		{
			Controller = controller;

			OnControllerReceived();
		}

		protected virtual void OnControllerReceived() { }
	}
}