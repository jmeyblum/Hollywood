
namespace Hollywood.Controller
{
	public interface IItemController
	{

	}

	public interface IItemController<T> : IItemController, IItemObserver<T> where T : class, IControlledItem
	{

	}

	public interface IItemController<T, U> : IItemController<T> where T : class, IControlledItem<U, T> where U : class, IItemController<T, U>
	{

	}
}