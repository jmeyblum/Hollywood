using System.Collections;
using System.Collections.Generic;

namespace Hollywood.Runtime
{
	public interface IInjectionContext : IAdvancedInjectionContext
	{
		T FindDependency<T>(object injected);
		T GetInstance<T>(object owner = null);
		IEnumerable<T> GetInstances<T>(object owner = null);
		void DisposeInstance(object instance);
		void Reset();
	}

	public interface IAdvancedInjectionContext : IInternalInjectionContext
	{
		T AddInstance<T>(object owner = null);
		IEnumerable<T> AddInstances<T>(object owner = null);
		void ResolveInstance(object instance);
		void ResolveInstances(IEnumerable instances);
	}

	public interface IInternalInjectionContext
	{
		void ResolveOwnedInstances(object owner);
		void DisposeOwnedInstances(object owner);
	}
}