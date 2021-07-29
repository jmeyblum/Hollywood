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
			Assert.IsNotNull(typeResolver, $"{nameof(typeResolver)} is null.");
			Assert.IsNotNull(instanceCreator, $"{nameof(instanceCreator)} is null.");

			TypeResolver = typeResolver;
			InstanceCreator = instanceCreator;
		}

		public DefaultInjectionContext()
		{ }

		T IInjectionContext.FindDependency<T>(object instance)
		{
			Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
			Assert.IsTrue(Instances.Contains(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");
			Assert.IsFalse(instance is IModule, $"{instance} implements {nameof(IModule)} and thus can't have dependencies.");
			Assert.IsFalse(typeof(T).IsInterface, $"{typeof(T).Name} is not an interface.");

			var parent = Instances.GetParent(instance);
			var child = instance;

			while (parent != null)
			{
				var dependency = FindInnerDependency<T>(parent, child);
				if(dependency != null)
				{
					return dependency;
				}

				child = parent;
				parent = Instances.GetParent(parent);
			}

			Assert.Throw($"No dependency of type {typeof(T).Name} found for instance: {instance}.");

			return default;
		}

		private T FindInnerDependency<T>(object current, object childToIgnore = null)
		{
			if (typeof(T).IsAssignableFrom(current.GetType()))
			{
				return (T)current;
			}

			foreach (var children in Instances.GetChildren(current))
			{
				if(children == childToIgnore)
				{
					continue;
				}

				if (typeof(T).IsAssignableFrom(children.GetType()))
				{
					return (T)children;
				}
				
				if (children is IModule module)
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
			Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
			Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

			if (instance is IDisposable disposable)
			{
				disposable.Dispose();
			}

			((IInternalInjectionContext)this).DisposeOwnedInstances(instance);			

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
			Assert.IsNotNull(TypeResolver, $"{nameof(TypeResolver)} is null.");
			Assert.IsNotNull(InstanceCreator, $"{nameof(InstanceCreator)} is null.");
			Assert.IsFalse(typeof(T).IsInterface, $"{typeof(T).Name} is not an interface.");

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

			Assert.IsNotNull(instanceType, $"{nameof(TypeResolver)} resolved to null for type {typeof(T).Name}.");
			Assert.IsTrue(typeof(T).IsAssignableFrom(instanceType), $"{nameof(TypeResolver)} resolved to an incompatible type ({instanceType.Name}) for type {typeof(T).Name}.");

			var instance = (T)InstanceCreator.Create(instanceType);

			Instances.Add(instance, owner);
			InstancesData.Add(instance, new InstanceData());

			return instance;
		}

		IEnumerable<T> IAdvancedInjectionContext.AddInstances<T>(object owner)
		{
			Assert.IsNotNull(TypeResolver, $"{nameof(TypeResolver)} is null.");
			Assert.IsNotNull(InstanceCreator, $"{nameof(InstanceCreator)} is null.");

			var existingInstances = Instances.GetChildren(owner).Where(child => typeof(T).IsAssignableFrom(child.GetType())).Cast<T>();
			var instanceTypes = TypeResolver.GetAll<T>();

			Assert.IsNotNull(instanceTypes, $"{nameof(TypeResolver)} resolved to null for type {typeof(T).Name}.");
			Assert.IsTrue(instanceTypes.Count() > 0, $"{nameof(TypeResolver)} resolved to 0 types for type {typeof(T).Name}.");
			Assert.IsTrue(instanceTypes.All(instanceType => typeof(T).IsAssignableFrom(instanceType)), $"{nameof(TypeResolver)} resolved to a some incompatible types for type {typeof(T).Name}.");

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
			Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
			Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

			var instanceData = InstancesData[instance];

			if (instanceData.Resolved)
			{
				return;
			}

			if (instance is __Hollywood_Injected injected)
			{
				injected.__Resolve();
			}

			((IInternalInjectionContext)this).ResolveOwnedInstances(instance);
			
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
			Assert.IsNotNull(instances, $"{nameof(instances)} is null.");

			foreach (var instance in instances)
			{
				((IInjectionContext)this).ResolveInstance(instance);
			}
		}

		void IInternalInjectionContext.ResolveOwnedInstances(object owner)
		{
			Assert.IsTrue(Instances.Contains(owner), $"{owner} is unknown from this {nameof(IInjectionContext)}: {this}.");

			foreach (var instance in Instances.GetChildren(owner))
			{
				((IAdvancedInjectionContext)this).ResolveInstance(instance);
			}
		}

		void IInternalInjectionContext.DisposeOwnedInstances(object owner)
		{
			Assert.IsTrue(Instances.Contains(owner), $"{owner} is unknown from this {nameof(IInjectionContext)}: {this}.");

			while (Instances.GetChildren(owner).Any())
			{
				var child = Instances.GetChildren(owner).First();

				((IInjectionContext)this).DisposeInstance(child);
			}
		}
	}
}