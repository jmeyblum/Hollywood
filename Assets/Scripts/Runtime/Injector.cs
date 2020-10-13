using Hollywood.Runtime.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hollywood.Runtime
{
	// TODO: Add attribute to ignore specific class from being resolved (non-abstract class implementing interface but only meant to be used by derived types)
	// TODO: Add API to set/create contexts
	// TODO: Think about custom instance creation (not Activator.CreateInstance)
	// TODO: Test performance of reflection calls
	// TODO: Add callbacks for system events (dependency types not found...)

	public static class Injector
	{
		private static IContext Context;
		private static Hierarchy<object> Instances = new Hierarchy<object>();
		private static Dictionary<object, InstanceData> InstancesData = new Dictionary<object, InstanceData>();

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
			Assert.IsTrue(Instances.Contains(injected));

			var parent = Instances.GetParent(injected);

			while (parent != null)
			{
				if (typeof(T).IsAssignableFrom(parent.GetType()))
				{
					return (T)parent;
				}

				foreach (var children in Instances.GetChildren(parent))
				{
					if (typeof(T).IsAssignableFrom(children.GetType()))
					{
						return (T)children;
					}
				}

				parent = Instances.GetParent(parent);
			}

			Debug.LogAssertion($"No {typeof(T)} found.");

			return default;
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
		public static T GetInstance<T>(IContext context = default, object owner = null)
		{
			Assert.IsFalse(Instances.Locked);

			var previousContext = Context;
			Context = context;

			T instance = default;
			try
			{
				instance = Advanced.AddInstance<T>(owner, context);

				Advanced.ResolveInstance(instance);
			}
			finally
			{
				Context = previousContext;
			}

			return instance;
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
		public static IEnumerable<T> GetInstances<T>(IContext context = default, object owner = null)
		{
			Assert.IsFalse(Instances.Locked);

			var previousContext = Context;
			Context = context;

			IEnumerable<T> instances = Enumerable.Empty<T>();
			try
			{
				instances = Advanced.AddInstances<T>(owner, context);

				Advanced.ResolveInstances(instances);
			}
			finally
			{
				Context = previousContext;
			}

			return instances;
		}

		/// <summary>
		/// Dispose this instance and all owned instances.
		/// This is usually called by the instance IDisposable.Dispose() method.
		/// </summary>
		/// <param name="instance"></param>
		public static void DisposeInstance(object instance)
		{
			Assert.IsTrue(Instances.Contains(instance));
			Assert.IsTrue(InstancesData.ContainsKey(instance));

			// DisposeInstance is called recursively through disposable children Dispose() method.
			// We don't want to remove children instances while iterating on them so we keep track if we are recursion depth 0 and only remove all instances at this point.
			bool currentlyLocked = Instances.Locked;

			if(instance is IDisposable disposable)
			{
				disposable.Dispose();
			}

			foreach (var child in Instances.GetChildren(instance))
			{
				Instances.Locked = true;

				if (child is IInjected injected)
				{
					injected.__Dispose();
				}

				Instances.Locked = currentlyLocked;
			}

			if (!currentlyLocked)
			{
				Instances.Remove(instance, recursively: true);
			}

			InstancesData.Remove(instance);
		}

		public static void Reset()
		{
			Context = null;

			Instances = new Hierarchy<object>();
			InstancesData = new Dictionary<object, InstanceData>();
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
			public static T AddInstance<T>(object owner, IContext context = default)
			{
				context = context ?? Context;

				if (owner != null)
				{
					foreach (var child in Instances.GetChildren(owner))
					{
						if (typeof(T).IsAssignableFrom(child.GetType()))
						{
							return (T)child;
						}
					}
				}

				var instanceType = context.Get<T>();

				Assert.IsNotNull(instanceType);
				// TODO: assert instance type is valid for T.

				var instance = (T)Activator.CreateInstance(instanceType, true);

				Instances.Add(instance, owner);
				InstancesData.Add(instance, new InstanceData());

				return instance;
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
			public static IEnumerable<T> AddInstances<T>(object owner, IContext context = default)
			{
				context = context ?? Context;

				var existingInstances = Instances.GetChildren(owner).Where(child => typeof(T).IsAssignableFrom(child.GetType())).Cast<T>();
				var instanceTypes = context.GetAll<T>();
				// TODO: assert all those types are valid for T.

				Assert.IsNotNull(instanceTypes);
				Assert.IsTrue(instanceTypes.Count() > 0);

				var instanceTypesToCreate = instanceTypes.Except(existingInstances.Select(c => c.GetType()));

				var instances = new List<T>();

				foreach (var instanceType in instanceTypesToCreate)
				{
					var instance = (T)Activator.CreateInstance(instanceType, true);
					instances.Add(instance);

					Instances.Add(instance, owner);
					InstancesData.Add(instance, new InstanceData());
				}

				return instances.Union(existingInstances);
			}

			/// <summary>
			/// Recursively resolves the specified instance.
			/// </summary>
			/// <param name="instance"></param>
			public static void ResolveInstance(object instance)
			{
				Assert.IsTrue(Instances.Contains(instance));
				Assert.IsTrue(InstancesData.ContainsKey(instance));

				var instanceData = InstancesData[instance];

				if (instanceData.Resolved)
				{
					return;
				}

				if (instance is IInjected injected)
				{
					injected.__Resolve();
				}
				else
				{
					Internal.ResolveOwnedInstances(instance);
				}

				if(instance is IResolvable resolvable)
				{
					resolvable.Resolve();
				}

				instanceData.Resolved = true;

				if(instance is IOnReadyListener onReadyListener)
				{
					onReadyListener.OnReady();
				}
			}

			/// <summary>
			/// Recursively resolves the specified instances.
			/// </summary>
			/// <param name="instances"></param>
			public static void ResolveInstances(IEnumerable instances)
			{
				Assert.IsNotNull(instances);

				foreach (var instance in instances)
				{
					ResolveInstance(instance);
				}
			}
		}

		public static class Internal
		{
			public static void ResolveOwnedInstances(object owner)
			{
				Assert.IsTrue(Instances.Contains(owner));

				foreach (var instance in Instances.GetChildren(owner))
				{
					Advanced.ResolveInstance(instance);
				}
			}

			public static void DisposeOwnedInstances(object owner)
			{
				Assert.IsTrue(Instances.Contains(owner));

				foreach (var instance in Instances.GetChildren(owner))
				{
					DisposeInstance(instance);
				}
			}
		}
	}
}