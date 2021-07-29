using Hollywood.Runtime.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

			var parent = Instances.GetParent(instance);
			var child = instance;

			T dependency = null;
			while (parent != null)
			{
				dependency = FindInnerDependency<T>(parent, child);
				if(dependency != null)
				{
					break;
				}

				child = parent;
				parent = Instances.GetParent(parent);
			}

			if (dependency is null)
			{
				Assert.Throw($"No dependency of type {typeof(T).Name} found for instance: {instance}.");
			} 
			else
			{
				Assert.IsTrue(InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

				var instanceData = InstancesData[instance];

				if (instanceData.State == InstanceState.Resolving)
				{
					instanceData.ResolvingNeeds ??= new HashSet<object>();
					instanceData.ResolvingNeeds.Add(dependency);
				}
			}

			return dependency;
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

			var instanceData = InstancesData[instance];
			instanceData.TaskTokenSource.Cancel();

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


		void IInjectionContext.Dispose()
		{
			((IInjectionContext)this).Reset();

			TypeResolver = null;
			InstanceCreator = null;
		}

		T IAdvancedInjectionContext.AddInstance<T>(object owner)
		{
			Assert.IsNotNull(TypeResolver, $"{nameof(TypeResolver)} is null.");
			Assert.IsNotNull(InstanceCreator, $"{nameof(InstanceCreator)} is null.");

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

			var instance = CreateNewInstance<T>(owner, instanceType);

			return instance;
		}

		IEnumerable<T> IAdvancedInjectionContext.AddInstances<T>(object owner)
		{
			Assert.IsNotNull(TypeResolver, $"{nameof(TypeResolver)} is null.");
			Assert.IsNotNull(InstanceCreator, $"{nameof(InstanceCreator)} is null.");

			var existingInstances = owner is null ? Enumerable.Empty<T>() : Instances.GetChildren(owner).Where(child => typeof(T).IsAssignableFrom(child.GetType())).Cast<T>();
			var instanceTypes = TypeResolver.GetAll<T>();

			Assert.IsNotNull(instanceTypes, $"{nameof(TypeResolver)} resolved to null for type {typeof(T).Name}.");
			Assert.IsTrue(instanceTypes.Count() > 0, $"{nameof(TypeResolver)} resolved to 0 types for type {typeof(T).Name}.");
			Assert.IsTrue(instanceTypes.All(instanceType => typeof(T).IsAssignableFrom(instanceType)), $"{nameof(TypeResolver)} resolved to a some incompatible types for type {typeof(T).Name}.");

			var instanceTypesToCreate = instanceTypes.Except(existingInstances.Select(c => c.GetType()));

			var instances = new List<T>();

			foreach (var instanceType in instanceTypesToCreate)
			{
				var instance = CreateNewInstance<T>(owner, instanceType);

				instances.Add(instance);
			}

			return instances.Union(existingInstances);
		}

		private T CreateNewInstance<T>(object owner, Type instanceType) where T : class
		{
			var instance = (T)InstanceCreator.Create(instanceType);

			Instances.Add(instance, owner);
			var instanceData = new InstanceData();
			InstancesData.Add(instance, instanceData);

			instanceData.TaskTokenSource = new CancellationTokenSource();
			instanceData.ResolvingTask = CreateInstanceDataResolvingTask(instance);
			instanceData.InitializationTask = CreateInstanceDataInitializationTask(instance);

			return instance;
		}

		void IAdvancedInjectionContext.ResolveInstance(object instance)
		{
			Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
			Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

			var instanceData = InstancesData[instance];

			if (instanceData.State > InstanceState.UnResolved)
			{
				return;
			}

			instanceData.State = InstanceState.Resolving;

			if (instance is __Hollywood_Injected injected)
			{
				injected.__Resolve();
			}

			((IInternalInjectionContext)this).ResolveOwnedInstances(instance);
			
			if (instance is IResolvable resolvable)
			{
				resolvable.Resolve();
			}

			instanceData.State = InstanceState.Initializing;
		}

		private void VerifyCycle(object instance, List<object> needs)
		{
			Assert.IsFalse(needs.Contains(instance), $"There is an initialization cycle introduced by a cyclic chain of needed dependencies: {string.Join(" -> ", needs)} -> {instance}");

			Assert.IsTrue(InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

			var instanceData = InstancesData[instance];

			needs.Add(instance);

			if (instanceData.ResolvingNeeds != null)
			{
				foreach (var dependency in instanceData.ResolvingNeeds)
				{
					VerifyCycle(dependency, needs);
				}
			}

			needs.Remove(instance);
		}

		private async Task CreateInstanceDataResolvingTask(object instance)
		{
			try
			{
				Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
				Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

				var instanceData = InstancesData[instance];

				while (instanceData.State < InstanceState.Initializing)
				{
					await Task.Yield();
					instanceData.TaskTokenSource.Token.ThrowIfCancellationRequested();
				}
			}
			catch (Exception e)
			{
				Log.LogFatalError(e);
			}
		}

		private async Task CreateInstanceDataInitializationTask(object instance)
		{
			try
			{
				Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
				Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

				var instanceData = InstancesData[instance];

				await instanceData.ResolvingTask;

				if (instanceData.ResolvingNeeds != null)
				{
					List<Task> resolvingTasks = new List<Task>();

					foreach (var dependency in instanceData.ResolvingNeeds)
					{
						Assert.IsTrue(InstancesData.ContainsKey(dependency), $"{dependency} is unknown from this {nameof(IInjectionContext)}: {this}.");

						var dependencyInstanceData = InstancesData[dependency];

						resolvingTasks.Add(dependencyInstanceData.ResolvingTask);
					}

					await Task.WhenAll(resolvingTasks);
					instanceData.TaskTokenSource.Token.ThrowIfCancellationRequested();

					VerifyCycle(instance, new List<object>());

					List<Task> initializationTasks = new List<Task>();

					foreach (var dependency in instanceData.ResolvingNeeds)
					{
						Assert.IsTrue(InstancesData.ContainsKey(dependency), $"{dependency} is unknown from this {nameof(IInjectionContext)}: {this}.");

						var dependencyInstanceData = InstancesData[dependency];

						initializationTasks.Add(dependencyInstanceData.InitializationTask);
					}

					await Task.WhenAll(initializationTasks);
				}

				instanceData.TaskTokenSource.Token.ThrowIfCancellationRequested();

				if (instance is IInitializable initializable)
				{
					await initializable.Initialize(instanceData.TaskTokenSource.Token);
				}

				instanceData.State = InstanceState.Initialized;
			} 
			catch(Exception e)
			{
				Log.LogFatalError(e);
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