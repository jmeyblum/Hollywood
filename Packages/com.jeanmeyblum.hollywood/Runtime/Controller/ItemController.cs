
namespace Hollywood.Controller
{
	public abstract class ItemController<T, U> : IItemController<T, U> where T : class, IControlledItem<U, T> where U : class, IItemController<T, U>
	{
		[Needs]
		private IAsserter Asserter;

		protected T Item { get; private set; }

		void IItemObserver<T>.OnItemCreated(T item)
		{
			Asserter?.IsNull(Item);

			Item = item;

			item.OnControllerReceived(this as U);

			OnItemCreated();
		}

		void IItemObserver<T>.OnItemDestroyed(T item)
		{
			Asserter?.IsTrue(Item == item);

			Item = null;

			OnItemDestroyed();
		}

		protected virtual void OnItemCreated() { }

		protected virtual void OnItemDestroyed() { }
	}
}