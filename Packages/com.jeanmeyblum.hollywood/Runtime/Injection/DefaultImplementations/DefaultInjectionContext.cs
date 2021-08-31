using Hollywood.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hollywood
{
	public class DefaultInjectionContext : IInjectionContext
	{
		public ITypeResolver TypeResolver;
		public IInstanceCreator InstanceCreator;

		private readonly Hierarchy<object> Instances = new Hierarchy<object>();
		private readonly Dictionary<object, InstanceData> InstancesData = new Dictionary<object, InstanceData>();
		private readonly Dictionary<Type, HashSet<object>> TypeToItemObservers = new Dictionary<Type, HashSet<object>>();
		private readonly Dictionary<object, Dictionary<Type, ItemObserverData>> ItemObserversData = new Dictionary<object, Dictionary<Type, ItemObserverData>>();

		public DefaultInjectionContext(ITypeResolver typeResolver, IInstanceCreator instanceCreator)
		{
			Assert.IsNotNull(typeResolver, $"{nameof(typeResolver)} is null.");
			Assert.IsNotNull(instanceCreator, $"{nameof(instanceCreator)} is null.");

			TypeResolver = typeResolver;
			InstanceCreator = instanceCreator;
		}

		public DefaultInjectionContext()
		{ }

		T IInjectionContext.FindDependency<T>(object instance, bool ignoreInitialization)
		{
			Log.LogTrace($"Finding dependency of type {typeof(T).Name} for {instance} ({instance?.GetType().Name}).");

			Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
			Assert.IsTrue(Instances.Contains(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");
			Assert.IsFalse(instance is IModule, $"{instance} implements {nameof(IModule)} and thus can't have dependencies.");

			var parent = instance;
			object child = null;

			T dependency = null;
			while (parent != null)
			{
				dependency = FindInnerDependency<T>(parent, child);
				if (dependency != null)
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
				Log.LogTrace($"Dependency {dependency} found for needed type {typeof(T).Name} for {instance} ({instance?.GetType().Name}).");

				Assert.IsTrue(InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

				var instanceData = InstancesData[instance];

				if (instanceData.State == InstanceState.Resolving)
				{
					instanceData.ResolvingNeeds ??= new Dictionary<object, bool>();
					instanceData.ResolvingNeeds[dependency] = ignoreInitialization;
				}
			}

			return dependency;
		}

		private T FindInnerDependency<T>(object current, object childToIgnore = null)
		{
			Type type = typeof(T);

			if (IsAssignableFrom(type, current.GetType()))
			{
				return (T)current;
			}

			foreach (var children in Instances.GetChildren(current))
			{
				if (children == childToIgnore)
				{
					continue;
				}

				if (IsAssignableFrom(type, children.GetType()))
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

		private bool IsAssignableFrom(Type generalType, Type specificType)
		{
			return TypeResolver.GetAssignableTypes(specificType).Contains(generalType);
		}

		T IInjectionContext.GetInstance<T>(object owner)
		{
			Log.LogTrace($"Getting instance of type {typeof(T).Name} for owner {owner} ({owner?.GetType().Name}).");

			var instance = ((IAdvancedInjectionContext)this).AddInstance<T>(owner);

			((IAdvancedInjectionContext)this).ResolveInstance(instance);

			return instance;
		}

		IEnumerable<T> IInjectionContext.GetInstances<T>(object owner)
		{
			Log.LogTrace($"Getting instances of type {typeof(T).Name} for owner {owner} ({owner?.GetType().Name}).");

			var instances = ((IAdvancedInjectionContext)this).AddInstances<T>(owner);

			((IAdvancedInjectionContext)this).ResolveInstances(instances);

			return instances;
		}

		void IInjectionContext.DisposeInstance(object instance)
		{
			Log.LogTrace($"Disposing instance {instance} ({instance?.GetType().Name}).");

			Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
			Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

			var instanceData = InstancesData[instance];
			instanceData.TaskTokenSource.Cancel();

			IInjectionContext previousInjectionContext = Injector.InjectionContext;
			try
			{
				Injector.InjectionContext = this;

				((IAdvancedInjectionContext)this).NotifyItemDestruction(instance);

				if (instance is __Hollywood_ItemObserver itemObserver)
				{
					Log.LogTrace($"Unregistering observed item instance {instance} ({instance?.GetType().Name}).");

					itemObserver.__Unregister();
				}

				if (instance is IDisposable disposable)
				{
					try
					{
						Log.LogTrace($"Disposing instance {instance} ({instance?.GetType().Name}).");

						disposable.Dispose();
					}
					catch (Exception e)
					{
						Log.LogError(e);
					}
				}

				((IInternalInjectionContext)this).DisposeOwnedInstances(instance);
			}
			catch (Exception e)
			{
				Log.LogFatalError(e);
			}
			finally
			{
				Injector.InjectionContext = previousInjectionContext;
			}

			Instances.Remove(instance, recursively: false);
			InstancesData.Remove(instance);
		}

		void IInjectionContext.Reset()
		{
			Log.LogTrace($"Resetting {nameof(IInjectionContext)}.");

			((IInternalInjectionContext)this).DisposeOwnedInstances(Instances.Root);

			TypeResolver.Reset();
			InstanceCreator.Reset();
			Instances.Reset();
			InstancesData.Clear();
			TypeToItemObservers.Clear();
			ItemObserversData.Clear();
		}

		void IInjectionContext.Dispose()
		{
			Log.LogTrace($"Disposing {nameof(IInjectionContext)}.");

			((IInjectionContext)this).Reset();

			TypeResolver = null;
			InstanceCreator = null;
		}

		T IAdvancedInjectionContext.AddInstance<T>(object owner)
		{
			Log.LogTrace($"Adding instance of type {typeof(T).Name} for owner {owner} ({owner?.GetType().Name}).");

			Assert.IsNotNull(TypeResolver, $"{nameof(TypeResolver)} is null.");
			Assert.IsNotNull(InstanceCreator, $"{nameof(InstanceCreator)} is null.");

			owner ??= Instances.Root;

			Type type = typeof(T);

			foreach (var child in Instances.GetChildren(owner))
			{
				if (IsAssignableFrom(type, child.GetType()))
				{
					Log.LogWarning($"An instance {child} of type {typeof(T).Name} already exists for owner {owner} ({owner?.GetType().Name}).");

					return (T)child;
				}
			}

			var instanceType = TypeResolver.Get<T>();

			Assert.IsNotNull(instanceType, $"{nameof(TypeResolver)} resolved to null for type {typeof(T).Name}.");
			Assert.IsTrue(IsAssignableFrom(type, instanceType), $"{nameof(TypeResolver)} resolved to an incompatible type ({instanceType.Name}) for type {typeof(T).Name}.");

			var instance = CreateNewInstance<T>(owner, instanceType);

			return instance;
		}

		void IAdvancedInjectionContext.AddExternalInstance<T>(T instance, object owner, bool autoResolve)
		{
			Log.LogTrace($"Adding external instance {instance} of type {typeof(T).Name} for owner {owner} ({owner?.GetType().Name}) ({nameof(autoResolve)}: {autoResolve}).");

			owner ??= Instances.Root;

			Type type = typeof(T);

			Assert.IsFalse(Instances.GetChildren(owner).Any(child => IsAssignableFrom(type, child.GetType())), $"{owner} already contains an instance for type {typeof(T).Name}.");

			SetupInstance<T>(owner, instance);

			if (autoResolve)
			{
				((IAdvancedInjectionContext)this).ResolveInstance(instance);
			}
		}

		IEnumerable<T> IAdvancedInjectionContext.AddInstances<T>(object owner)
		{
			Log.LogTrace($"Adding instances of type {typeof(T).Name} for owner {owner} ({owner?.GetType().Name}).");

			Assert.IsNotNull(TypeResolver, $"{nameof(TypeResolver)} is null.");
			Assert.IsNotNull(InstanceCreator, $"{nameof(InstanceCreator)} is null.");

			owner ??= Instances.Root;

			Type type = typeof(T);

			var existingInstances = Instances.GetChildren(owner).Where(child => IsAssignableFrom(type, child.GetType())).Cast<T>();
			var instanceTypes = TypeResolver.GetAll<T>();

			Assert.IsNotNull(instanceTypes, $"{nameof(TypeResolver)} resolved to null for type {typeof(T).Name}.");
			Assert.IsTrue(instanceTypes.Count() > 0, $"{nameof(TypeResolver)} resolved to 0 types for type {typeof(T).Name}.");
			Assert.IsTrue(instanceTypes.All(instanceType => IsAssignableFrom(type, instanceType)), $"{nameof(TypeResolver)} resolved to a some incompatible types for type {typeof(T).Name}.");

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
			Log.LogTrace($"Creating new instance of type {instanceType?.Name} for type {typeof(T).Name} for owner {owner} ({owner?.GetType().Name}).");

			var instance = (T)InstanceCreator.Create(instanceType);

			SetupInstance(owner, instance);

			return instance;
		}

		private void SetupInstance<T>(object owner, T instance) where T : class
		{
			Log.LogTrace($"Creating instance internal data for {instance} ({instance?.GetType().Name}) owned by {owner} ({owner?.GetType().Name}).");

			Instances.Add(instance, owner);
			var instanceData = new InstanceData();
			InstancesData.Add(instance, instanceData);

			instanceData.TaskTokenSource = new CancellationTokenSource();
			instanceData.ResolvingTask = CreateInstanceDataResolvingTask(instance);
			instanceData.InitializationTask = CreateInstanceDataInitializationTask(instance);
		}

		void IAdvancedInjectionContext.ResolveInstance(object instance)
		{
			Log.LogTrace($"Resolving instance {instance} ({instance?.GetType().Name}).");

			Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
			Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

			var instanceData = InstancesData[instance];

			if (instanceData.State > InstanceState.UnResolved)
			{
				return;
			}

			Log.LogTrace($"Instance {instance} ({instance?.GetType().Name}) changing state from {instanceData?.State} to {InstanceState.Resolving}.");

			instanceData.State = InstanceState.Resolving;

			IInjectionContext previousInjectionContext = Injector.InjectionContext;
			try
			{
				Injector.InjectionContext = this;

				if (instance is __Hollywood_Injected injected)
				{
					injected.__Resolve();
				}

				((IInternalInjectionContext)this).ResolveOwnedInstances(instance);

				if (instance is IResolvable resolvable)
				{
					try
					{
						resolvable.Resolve();
					}
					catch (Exception e)
					{
						Log.LogError(e);
					}
				}
			}
			catch (Exception e)
			{
				Log.LogFatalError(e);
			}
			finally
			{
				Injector.InjectionContext = previousInjectionContext;
			}

			Log.LogTrace($"Instance {instance} ({instance?.GetType().Name}) changing state from {instanceData?.State} to {InstanceState.Initializing}.");

			instanceData.State = InstanceState.Initializing;
		}

		private async Task CreateInstanceDataResolvingTask(object instance)
		{
			try
			{
				Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
				Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

				var instanceData = InstancesData[instance];
				var token = instanceData.TaskTokenSource.Token;

				while (instanceData.State < InstanceState.Initializing)
				{
					await Task.Yield();
					token.ThrowIfCancellationRequested();
				}
			}
			catch (TaskCanceledException) { }
			catch (OperationCanceledException) { }
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
				var token = instanceData.TaskTokenSource.Token;

				await instanceData.ResolvingTask;

				if (instanceData.ResolvingNeeds != null)
				{
					List<Task> resolvingTasks = new List<Task>();

					foreach (var dependencyKVP in instanceData.ResolvingNeeds)
					{
						var dependency = dependencyKVP.Key;

						Log.LogTrace($"Instance {instance} ({instance?.GetType().Name}) is waiting for resolution of dependency {dependency} ({dependency?.GetType().Name}).");

						Assert.IsTrue(InstancesData.ContainsKey(dependency), $"{dependency} is unknown from this {nameof(IInjectionContext)}: {this}.");

						var dependencyInstanceData = InstancesData[dependency];

						resolvingTasks.Add(dependencyInstanceData.ResolvingTask);
					}

					await Task.WhenAll(resolvingTasks);
					token.ThrowIfCancellationRequested();

					Log.LogTrace($"Verifying dependency cycle for instance {instance} ({instance?.GetType().Name}).");

					VerifyCycle(instance, new List<object>());

					List<Task> initializationTasks = new List<Task>();

					foreach (var dependencyKVP in instanceData.ResolvingNeeds)
					{
						var dependency = dependencyKVP.Key;
						var ignoreInitialization = dependencyKVP.Value;

						if (!ignoreInitialization)
						{
							Log.LogTrace($"Instance {instance} ({instance?.GetType().Name}) is waiting for initialization of dependency {dependency} ({dependency?.GetType().Name}).");

							Assert.IsTrue(InstancesData.ContainsKey(dependency), $"{dependency} is unknown from this {nameof(IInjectionContext)}: {this}.");

							var dependencyInstanceData = InstancesData[dependency];

							initializationTasks.Add(dependencyInstanceData.InitializationTask);
						}
					}

					await Task.WhenAll(initializationTasks);
				}

				token.ThrowIfCancellationRequested();

				if (instance is IInitializable initializable)
				{
					Log.LogTrace($"Initializing instance {instance} ({instance?.GetType().Name}).");

					try
					{
						await initializable.Initialize(token);
					}
					catch (TaskCanceledException) { }
					catch (OperationCanceledException) { }
					catch (Exception e)
					{
						Log.LogError(e);
					}

					Log.LogTrace($"Done initializing instance {instance} ({instance?.GetType().Name}).");
				}

				IInjectionContext previousInjectionContext = Injector.InjectionContext;
				try
				{
					Injector.InjectionContext = this;

					if (instance is __Hollywood_ItemObserver itemObserver)
					{
						Log.LogTrace($"Registering observed item instance {instance} ({instance?.GetType().Name}).");

						itemObserver.__Register();
					}

					if (instance is IUpdatable updatable)
					{
						CreateUpdatableTask(updatable);
					}
				}
				catch (Exception e)
				{
					Log.LogFatalError(e);
				}
				finally
				{
					Injector.InjectionContext = previousInjectionContext;
				}

				Log.LogTrace($"Instance {instance} ({instance?.GetType().Name}) changing state from {instanceData?.State} to {InstanceState.Initialized}.");

				instanceData.State = InstanceState.Initialized;

				((IAdvancedInjectionContext)this).NotifyItemCreation(instance);
			}
			catch (TaskCanceledException) { }
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				Log.LogFatalError(e);
			}

			void VerifyCycle(object instance, List<object> needs)
			{
				Assert.IsFalse(needs.Contains(instance), $"There is an initialization cycle introduced by a cyclic chain of needed dependencies: {string.Join(" -> ", needs)} -> {instance}");
				Assert.IsTrue(InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

				var instanceData = InstancesData[instance];

				needs.Add(instance);

				if (instanceData.ResolvingNeeds != null)
				{
					foreach (var dependencyKVP in instanceData.ResolvingNeeds)
					{
						var ignoreInitialization = dependencyKVP.Value;
						if (!ignoreInitialization)
						{
							VerifyCycle(dependencyKVP.Key, needs);
						}
					}
				}

				needs.Remove(instance);
			}

			async void CreateUpdatableTask(IUpdatable instance)
			{
				try
				{
					Log.LogTrace($"Starting update for instance {instance} ({instance?.GetType().Name}).");

					Assert.IsNotNull(instance, $"{nameof(instance)} is null.");
					Assert.IsTrue(Instances.Contains(instance) && InstancesData.ContainsKey(instance), $"{instance} is unknown from this {nameof(IInjectionContext)}: {this}.");

					var instanceData = InstancesData[instance];

					var token = instanceData.TaskTokenSource.Token;

					await instance.Update(token);
				}
				catch (TaskCanceledException) { }
				catch (OperationCanceledException) { }
				catch (Exception e)
				{
					Log.LogError(e);
				}
				finally
				{
					Log.LogTrace($"Update task done for instance {instance} ({instance?.GetType().Name}).");
				}
			}
		}

		void IAdvancedInjectionContext.ResolveInstances(IEnumerable instances)
		{
			Log.LogTrace($"Resolving instances {instances}.");

			Assert.IsNotNull(instances, $"{nameof(instances)} is null.");

			foreach (var instance in instances)
			{
				((IInjectionContext)this).ResolveInstance(instance);
			}
		}

		void IAdvancedInjectionContext.NotifyItemCreation(object instance)
		{
			Log.LogTrace($"Notifying creation of instance {instance} ({instance?.GetType().Name}).");

			foreach (var type in TypeResolver.GetAssignableTypes(instance.GetType()))
			{
				if (TypeToItemObservers.TryGetValue(type, out var observers))
				{
					foreach (var observer in observers)
					{
						Log.LogTrace($"Notifying creation of instance {instance} ({instance?.GetType().Name}) to observer {observer} ({observer?.GetType().Name}).");

						try
						{
							ItemObserversData[observer][type].OnItemCreated(instance);
						}
						catch (Exception e)
						{
							Log.LogError(e);
						}
					}
				}
			}
		}

		void IAdvancedInjectionContext.NotifyItemDestruction(object instance)
		{
			Log.LogTrace($"Notifying destruction of instance {instance} ({instance?.GetType().Name}).");

			foreach (var type in TypeResolver.GetAssignableTypes(instance.GetType()))
			{
				if (TypeToItemObservers.TryGetValue(type, out var observers))
				{
					foreach (var observer in observers)
					{
						try
						{
							Log.LogTrace($"Notifying destruction of instance {instance} ({instance?.GetType().Name}) to observer {observer} ({observer?.GetType().Name}).");

							ItemObserversData[observer][type].OnItemDestroyed(instance);
						}
						catch (Exception e)
						{
							Log.LogError(e);
						}
					}
				}
			}
		}

		void IInternalInjectionContext.ResolveOwnedInstances(object owner)
		{
			Log.LogTrace($"Resolving owner {owner} ({owner?.GetType().Name}) owned instances.");

			Assert.IsTrue(Instances.Contains(owner), $"{owner} is unknown from this {nameof(IInjectionContext)}: {this}.");

			foreach (var instance in Instances.GetChildren(owner))
			{
				((IAdvancedInjectionContext)this).ResolveInstance(instance);
			}
		}

		void IInternalInjectionContext.DisposeOwnedInstances(object owner)
		{
			Log.LogTrace($"Disposing owner {owner} ({owner?.GetType().Name}) owned instances.");

			Assert.IsTrue(Instances.Contains(owner), $"{owner} is unknown from this {nameof(IInjectionContext)}: {this}.");

			while (Instances.GetChildren(owner).Any())
			{
				var child = Instances.GetChildren(owner).First();

				((IInjectionContext)this).DisposeInstance(child);
			}
		}

		void IInternalInjectionContext.RegisterItemObserver<T>(IItemObserver<T> observer)
		{
			Log.LogTrace($"Registering item observer {observer} ({observer?.GetType().Name}) for type {typeof(T).Name}.");

			var type = typeof(T);

			if (!TypeToItemObservers.TryGetValue(type, out var observers))
			{
				observers = new HashSet<object>();
				TypeToItemObservers[type] = observers;
			}

			observers.Add(observer);

			if (!ItemObserversData.TryGetValue(observer, out var typeToItemObserverData))
			{
				typeToItemObserverData = new Dictionary<Type, ItemObserverData>();
				ItemObserversData[observer] = typeToItemObserverData;
			}

			typeToItemObserverData[type] = new ItemObserverData<T>(observer);
		}

		void IInternalInjectionContext.UnregisterItemObserver<T>(IItemObserver<T> observer)
		{
			Log.LogTrace($"Unregistering item observer {observer} ({observer?.GetType().Name}) for type {typeof(T).Name}.");

			var type = typeof(T);

			if (!TypeToItemObservers.TryGetValue(type, out var observers))
			{
				observers.Remove(observer);

				if (!ItemObserversData.TryGetValue(observer, out var typeToItemObserverData))
				{
					typeToItemObserverData.Remove(type);

					if (typeToItemObserverData.Count == 0)
					{
						ItemObserversData.Remove(observer);
					}
				}
			}
		}
	}
}