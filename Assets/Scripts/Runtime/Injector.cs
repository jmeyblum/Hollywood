using Hollywood.Runtime.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hollywood.Runtime
{
    // TODO: Refactor IInjectable to set an owner, also add the onwed instance to the caller __ownedInstances. Doing so we should be able to get rid of the Owners stack, ResolveDependency<T>() should take the caller as a parameter and go up the owner chain (skiping the caller). ResolveOwnedInstances(IOwner owner) shouldn't have the push pop of owner. 

    // TODO: Add callbacks for post dependency resolution (using interfaces) and also when disposing
    // TODO: Implement OwnsAll
    // TODO: Add attribute to ignore specific class from being resolved (non-abstract class implementing interface but only meant to be used by derived types)
    // TODO: Add API to set/create contexts
    // TODO: Think about custom instance creation (not Activator.CreateInstance)
    // TODO: Think about dynamic resolution. Creating instances at runtime.
    // TODO: Test performance of reflection calls
    // TODO: Add callbacks for system events (dependency types not found...)

    public static class Injector
    {
        [UnityEditor.MenuItem("Jean/context")]
        public static void DoLoad()
        {
            var t = new ReflectionContext();
        }

        private static IContext Context;
        private static Stack<object> Owners = new Stack<object>();

        public static T ResolveDependency<T>()
        {
            foreach (var owner in Owners)
            {
                if (typeof(T).IsAssignableFrom(owner.GetType()))
                {
                    return (T)owner;
                }
                if (owner is IOwner iOwner && iOwner.__ownedInstances != null)
                {
                    foreach (var instance in iOwner.__ownedInstances)
                    {
                        if (typeof(T).IsAssignableFrom(instance.GetType()))
                        {
                            return (T)instance;
                        }
                    }
                }
            }

            Debug.LogAssertion($"No {typeof(T)} found.");

            return default;
        }

        public static T Instantiate<T>(IContext context = default, object owner = null) where T : class
		{
            var previousContext = Context;
            Context = context;

            T instance = default;
            try
            {
                instance = (T)Activator.CreateInstance(typeof(T), true);

                if (instance is IInjectable iInjectable)
                {
                    iInjectable.__ResolveDependencies();
                }
            }
            finally
			{
                Context = previousContext;
            }

            return instance;
		}

        public static void CreateOwnedInstance<T>(IOwner owner, IContext context = default)
		{
			if (owner.__ownedInstances != null)
			{
				foreach (var obj in owner.__ownedInstances)
				{
					if (typeof(T).IsAssignableFrom(obj.GetType()))
					{
						return;
					}
				}
			}
			else
			{
				owner.__ownedInstances = new HashSet<object>();
			}

			T instance = CreateInstance<T>(context);

			owner.__ownedInstances.Add(instance);
		}

		private static T CreateInstance<T>(IContext context = default)
		{
			context = context ?? Context;

			var instanceType = context.Get<T>();

			Assert.IsNotNull(instanceType);
			// TODO: assert instance type is valid for T.

			return (T)Activator.CreateInstance(instanceType, true);
		}

		public static void ResolveOwnedInstances(IOwner owner)
        {
            Owners.Push(owner);

            if (owner.__ownedInstances != null)
            {
                foreach (var instance in owner.__ownedInstances)
                {
                    if (instance is IInjectable injectable)
                    {
                        injectable.__ResolveDependencies();
                    }
                }
            }

            Owners.Pop();
        }

        public static void DisposeOwnedInstances(IOwner owner)
		{
            if (owner.__ownedInstances != null)
            {
                foreach (var instance in owner.__ownedInstances)
                {
                    if (instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
		}
    }
}