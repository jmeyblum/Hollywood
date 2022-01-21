
namespace Hollywood.Controller
{
	public abstract class ControlledItem<T, U> : IControlledItem<T, U> where T : class, IItemController<U, T> where U : class, IControlledItem<T, U>
	{
		protected T Controller { get; private set; }

		void IControlledItem<T>.OnControllerReceived(T controller)
		{
			Controller = controller;

			OnControllerReceived();
		}

		protected virtual void OnControllerReceived() { }
	}
}