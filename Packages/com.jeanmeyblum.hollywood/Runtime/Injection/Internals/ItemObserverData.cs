using System;
using System.Reflection;

namespace Hollywood.Internal
{
	public abstract class ItemObserverData
	{
		public abstract void OnItemCreated(object item);
		public abstract void OnItemDestroyed(object item);
	}

	public class ItemObserverData<T> : ItemObserverData where T : class
	{
		private static readonly MethodInfo OnItemCreatedMethodInfo = typeof(IItemObserver<T>).GetMethod(nameof(IItemObserver<T>.OnItemCreated), BindingFlags.Instance | BindingFlags.Public);
		private static readonly MethodInfo OnItemDestroyMethodInfo = typeof(IItemObserver<T>).GetMethod(nameof(IItemObserver<T>.OnItemDestroyed), BindingFlags.Instance | BindingFlags.Public);

		private readonly Action<T> OnItemCreatedDelegate;
		private readonly Action<T> NotifyDestructionDelegate;

		public ItemObserverData(IItemObserver<T> observer)
		{
			OnItemCreatedDelegate = (Action<T>)OnItemCreatedMethodInfo.CreateDelegate(typeof(Action<T>), observer);
			NotifyDestructionDelegate = (Action<T>)OnItemDestroyMethodInfo.CreateDelegate(typeof(Action<T>), observer);
		}

		public override void OnItemCreated(object item)
		{
			OnItemCreatedDelegate(item as T);
		}

		public override void OnItemDestroyed(object item)
		{
			NotifyDestructionDelegate(item as T);
		}
	}
}
