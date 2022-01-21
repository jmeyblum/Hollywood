
namespace Hollywood.Controller
{
	public interface IControlledItem
	{

	}

	public interface IControlledItem<T> : IControlledItem where T : class, IItemController
	{
		void OnControllerReceived(T controller);
	}

	public interface IControlledItem<T, U> : IControlledItem<T> where T : class, IItemController<U, T> where U : class, IControlledItem<T, U>
	{

	}
}