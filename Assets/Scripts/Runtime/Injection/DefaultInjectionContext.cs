using Hollywood.Runtime.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hollywood.Runtime
{
	public class DefaultInjectionContext : IInjectionContext
	{
		public ITypeResolver TypeResolver;
		public IInstanceCreator InstanceCreator;

		private readonly Hierarchy<object> Instances = new Hierarchy<object>();
		private readonly Dictionary<object, InstanceData> InstancesData = new Dictionary<object, InstanceData>();

		public DefaultInjectionContext(ITypeResolver typeResolver, IInstanceCreator instanceCreator)
		{
			Assert.IsNotNull(typeResolver);
			Assert.IsNotNull(instanceCreator);

			TypeResolver = typeResolver;
			InstanceCreator = instanceCreator;
		}

		public DefaultInjectionContext()
		{ }

		T IInjectionContext.FindDependency<T>(object instance)
		{
			Assert.IsNotNull(instance);
			Assert.IsTrue(Instances.Contains(instance));
			Assert.IsFalse(instance is IModule);

			var parent = Instances.GetParent(instance);

			while (parent != null)
			{
				var dependency = FindInnerDependency<T>(parent);
				if(dependency != null)
				{
					return dependency;
				}

				parent = Instances.GetParent(parent);
			}

			Assert.Throw($"No {typeof(T)} found.");

			return default;
		}

		private T FindInnerDependency<T>(object current)
		{
			if (typeof(T).IsAssignableFrom(current.GetType()))
			{
				return (T)current;
			}

			foreach (var children in Instances.GetChildren(current))
			{
				if (typeof(T).IsAssignableFrom(children.GetType()))
				{
					return (T)children;
				}

				var module = children as IModule;

				while (module != null)
				{
					foreach (var moduleChildren in Instances.GetChildren(module))
					{
						var dependency = FindInnerDependency<T>(moduleChildren);
						if (dependency != null)
						{
							return dependency;
						}
					}
				}
			}

			return default;
		}

		T IInjectionContext.GetInstance<T>(object owner)
		{
			var instance = ((IAdvancedInjectionContext)this).AddInstance<T>(owner);

			((IAdvancedInjectionContext)this).ResolveInstance(instance);

			return instance;
		}

		IEnumerable<T> IInjectionContext.GetInstances<T>(object owner)
		{
			var instances = ((IAdvancedInjectionContext)this).AddInstances<T>(owner);

			((IAdvancedInjectionContext)this).ResolveInstances(instances);

			return instances;
		}

		void IInjectionContext.DisposeInstance(object instance)
		{
			Assert.IsTrue(Instances.Contains(instance));
			Assert.IsTrue(InstancesData.ContainsKey(instance));

			if (instance is IDisposable disposable)
			{
				disposable.Dispose();
			}

			if (instance is IInjected injected)
			{
				injected.__Dispose();
			}
			else
			{
				((IInternalInjectionContext)this).DisposeOwnedInstances(instance);
			}

			Instances.Remove(instance, recursively: false);
			InstancesData.Remove(instance);
		}

		void IInjectionContext.Reset()
		{
			TypeResolver.Reset();
			InstanceCreator.Reset();
			Instances.Reset();
			InstancesData.Clear();
		}

		T IAdvancedInjectionContext.AddInstance<T>(object owner)
		{
			Assert.IsNotNull(TypeResolver);
			Assert.IsNotNull(InstanceCreator);

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

			var instanceType = TypeResolver.Get<T>();

			Assert.IsNotNull(instanceType);
			// TODO: assert instance type is valid for T.

			var instance = (T)InstanceCreator.Create(instanceType);

			Instances.Add(instance, owner);
			InstancesData.Add(instance, new InstanceData());

			return instance;
		}

		IEnumerable<T> IAdvancedInjectionContext.AddInstances<T>(object owner)
		{
			Assert.IsNotNull(TypeResolver);
			Assert.IsNotNull(InstanceCreator);

			var existingInstances = Instances.GetChildren(owner).Where(child => typeof(T).IsAssignableFrom(child.GetType())).Cast<T>();
			var instanceTypes = TypeResolver.GetAll<T>();
			// TODO: assert all those types are valid for T.

			Assert.IsNotNull(instanceTypes);
			Assert.IsTrue(instanceTypes.Count() > 0);

			var instanceTypesToCreate = instanceTypes.Except(existingInstances.Select(c => c.GetType()));

			var instances = new List<T>();

			foreach (var instanceType in instanceTypesToCreate)
			{
				var instance = (T)InstanceCreator.Create(instanceType);
				instances.Add(instance);

				Instances.Add(instance, owner);
				InstancesData.Add(instance, new InstanceData());
			}

			return instances.Union(existingInstances);
		}

		void IAdvancedInjectionContext.ResolveInstance(object instance)
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
				((IInternalInjectionContext)this).ResolveOwnedInstances(instance);
			}

			if (instance is IResolvable resolvable)
			{
				resolvable.Resolve();
			}

			instanceData.Resolved = true;

			if (instance is IOnReadyListener onReadyListener)
			{
				onReadyListener.OnReady();
			}
		}

		void IAdvancedInjectionContext.ResolveInstances(IEnumerable instances)
		{
			Assert.IsNotNull(instances);

			foreach (var instance in instances)
			{
				((IInjectionContext)this).ResolveInstance(instance);
			}
		}

		void IInternalInjectionContext.ResolveOwnedInstances(object owner)
		{
			Assert.IsTrue(Instances.Contains(owner));

			foreach (var instance in Instances.GetChildren(owner))
			{
				((IAdvancedInjectionContext)this).ResolveInstance(instance);
			}
		}

		void IInternalInjectionContext.DisposeOwnedInstances(object owner)
		{
			Assert.IsTrue(Instances.Contains(owner));

			while (Instances.GetChildren(owner).Any())
			{
				var child = Instances.GetChildren(owner).First();

				((IInjectionContext)this).DisposeInstance(child);
			}
		}
	}
}