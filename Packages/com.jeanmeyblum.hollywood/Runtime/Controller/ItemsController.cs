using System.Collections.Generic;

namespace Hollywood.Controller
{
	public abstract class ItemsController<T, U> : IItemController<T, U> where T : class, IControlledItem<U, T> where U : class, IItemController<T, U>
	{
		[Needs]
		private IAsserter Asserter;

		private HashSet<T> ItemsBackingField = new HashSet<T>();
		protected IReadOnlyCollection<T> Items => ItemsBackingField;

		void IItemObserver<T>.OnItemCreated(T item)
		{
			Asserter?.IsFalse(ItemsBackingField.Contains(item));

			ItemsBackingField.Add(item);

			item.OnControllerReceived(this as U);

			OnItemCreated();
		}

		void IItemObserver<T>.OnItemDestroyed(T item)
		{
			Asserter?.IsTrue(ItemsBackingField.Contains(item));

			ItemsBackingField.Remove(item);

			OnItemDestroyed();
		}

		protected virtual void OnItemCreated() { }

		protected virtual void OnItemDestroyed() { }
	}
}