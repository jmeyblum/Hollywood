﻿using System.Collections;
using System.Collections.Generic;

namespace Hollywood.Runtime
{
	// TODO: Add attribute to ignore specific class from being resolved (non-abstract class implementing interface but only meant to be used by derived types)
	// TODO: Add API to set/create contexts
	// TODO: Think about custom instance creation (not Activator.CreateInstance)
	// TODO: Test performance of reflection calls
	// TODO: Add callbacks for system events (dependency types not found...)

	public static class Injector
	{
		public static IInjectionContext InjectionContext;

		/// <summary>
		/// Finds a dependency of type T for the given object in parameter.
		/// The object looking for a dependency must have been created by the Injector.
		/// The dependency will be search in the owner ancestors of the object looking for it.
		/// </summary>
		/// <typeparam name="T">Type of the dependency.</typeparam>
		/// <param name="injected">The object looking for a dependency.</param>
		/// <returns></returns>
		public static T FindDependency<T>(object injected)
		{
			Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

			return InjectionContext.FindDependency<T>(injected);
		}

		/// <summary>
		/// Finds or creates an instance of type T that will be owned by the optional owner in parameter.
		/// The created instance will be immediately resolved.
		/// If you want to delay resolution, you can use Advanced.AddInstance<T>() instead and call manually Advanced.ResolveInstance() on the returned instance. 
		/// This might be needed if different instances owned by the same owner have a dependency with each other.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T GetInstance<T>(object owner = null)
		{
			Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

			return InjectionContext.GetInstance<T>(owner);
		}

		/// <summary>
		/// Finds or creates instances of type T that will be owned by the optional owner in parameter.
		/// The created instances will be immediately resolved.
		/// If you want to delay resolution, you can use Advanced.AddInstances<T>() instead and call manually Advanced.ResolveInstances() on the returned instance. 
		/// This might be needed if different instances owned by the same owner have a dependency with each other.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static IEnumerable<T> GetInstances<T>(object owner = null)
		{
			Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

			return InjectionContext.GetInstances<T>(owner);
		}

		/// <summary>
		/// Dispose this instance and all owned instances.
		/// This is usually called by the instance IDisposable.Dispose() method.
		/// </summary>
		/// <param name="instance"></param>
		public static void DisposeInstance(object instance)
		{
			Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

			InjectionContext.DisposeInstance(instance);
		}

		public static void Reset()
		{
			Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

			InjectionContext.Reset();
		}

		public static class Advanced
		{
			/// <summary>
			/// Adds an instance of type T for the specified optional owned.
			/// This will return any existing instance that already matches the given type.
			/// Instances created here are not automatically resolved. Use ResolveInstance() to do so.
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="owner"></param>
			/// <param name="context"></param>
			/// <returns></returns>
			public static T AddInstance<T>(object owner)
			{
				Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

				return InjectionContext.AddInstance<T>(owner);
			}

			/// <summary>
			/// Adds instances of type T for the specified optional owned.
			/// This will return any existing instances that already matches the given type and new instances created from other concrete types given by the context that match T.
			/// Instances created here are not automatically resolved. Use ResolveInstances() to do so.
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="owner"></param>
			/// <param name="context"></param>
			/// <returns></returns>
			public static IEnumerable<T> AddInstances<T>(object owner)
			{
				Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

				return InjectionContext.AddInstances<T>(owner);
			}

			/// <summary>
			/// Recursively resolves the specified instance.
			/// </summary>
			/// <param name="instance"></param>
			public static void ResolveInstance(object instance)
			{
				Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

				InjectionContext.ResolveInstance(instance);
			}

			/// <summary>
			/// Recursively resolves the specified instances.
			/// </summary>
			/// <param name="instances"></param>
			public static void ResolveInstances(IEnumerable instances)
			{
				Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

				InjectionContext.ResolveInstances(instances);
			}
		}

		public static class Internal
		{
			public static void ResolveOwnedInstances(object owner)
			{
				Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

				InjectionContext.ResolveOwnedInstances(owner);
			}

			public static void DisposeOwnedInstances(object owner)
			{
				Assert.IsNotNull(InjectionContext, $"No {nameof(InjectionContext)} defined.");

				InjectionContext.DisposeOwnedInstances(owner);
			}
		}
	}
}