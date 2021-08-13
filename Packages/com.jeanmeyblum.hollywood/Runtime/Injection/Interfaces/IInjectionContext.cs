using System.Collections;
using System.Collections.Generic;

namespace Hollywood.Runtime
{
	public interface IInjectionContext : IAdvancedInjectionContext
	{
		T FindDependency<T>(object instance, bool ignoreInitialization = false) where T : class;
		T GetInstance<T>(object owner = null) where T : class;
		IEnumerable<T> GetInstances<T>(object owner = null) where T : class;
		void DisposeInstance(object instance);
		void Reset();
		void Dispose();
	}

	public interface IAdvancedInjectionContext : IInternalInjectionContext
	{
		T AddInstance<T>(object owner = null) where T : class;
		void AddExternalInstance<T>(T instance, object owner = null, bool autoResolve = true) where T : class;
		IEnumerable<T> AddInstances<T>(object owner = null) where T : class;
		void ResolveInstance(object instance);
		void ResolveInstances(IEnumerable instances);
		void NotifyItemCreation(object instance);
		void NotifyItemDestruction(object instance);
	}

	public interface IInternalInjectionContext
	{
		void ResolveOwnedInstances(object owner);
		void DisposeOwnedInstances(object owner);
		void RegisterItemObserver<T>(IItemObserver<T> observer) where T : class;
		void UnregisterItemObserver<T>(IItemObserver<T> observer) where T : class;
	}
}