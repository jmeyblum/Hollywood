﻿using System.Collections;
using System.Collections.Generic;

namespace Hollywood.Runtime
{
	public interface IInjectionContext : IAdvancedInjectionContext
	{
		T FindDependency<T>(object instance) where T : class;
		T GetInstance<T>(object owner = null) where T : class;
		IEnumerable<T> GetInstances<T>(object owner = null) where T : class;
		void DisposeInstance(object instance);
		void Reset();
	}

	public interface IAdvancedInjectionContext : IInternalInjectionContext
	{
		T AddInstance<T>(object owner = null) where T : class;
		IEnumerable<T> AddInstances<T>(object owner = null) where T : class;
		void ResolveInstance(object instance);
		void ResolveInstances(IEnumerable instances);
	}

	public interface IInternalInjectionContext
	{
		void ResolveOwnedInstances(object owner);
		void DisposeOwnedInstances(object owner);
	}
}