# Hollywood

Hollywood is a dependency injection framework primarily made for Unity.

## Installation

Modify the `manifest.json` file of your Unity project under the Packages folder and add a the dependency to the package:

```json
{
  "dependencies": {
    "com.jeanmeyblum.hollywood": "git+https://github.com/jmeyblum/Hollywood?path=Packages/com.jeanmeyblum.hollywood",
  }
}
```

## Setup

Though this framework is primarily made for Unity, the framework is divided into two kind of assemblies, one requiring Unity references and the other working without dependencies to the Unity engine. The goal is to make it agnostic enough so it could be forked and used in a non-Unity project without too much work.

This documentation though will assume a setup for a Unity project.

### Automatic Setup

Simply add the following scripting define symbol to your project's player setting:

- HOLLYWOOD_UNITY_AUTO_SETUP

This will create a new ­­­­­`Injection Context` each time you enter play mode which already contains a `ILogger` and a `IAsserter` instance which are set also as the logger and asserter for the framework operations.

### Manual Setup

Before your application try to use the framework, you need to assign the static injection context instance to `Hollywood.Injector.InjectionContext`. You can do so yourself or make use of the helper methods find in `Hollywood.Helper` and `Hollywood.Unity.Helper`.

The easiest way to do so is to call `Hollywood.Unity.Helper.InitializeHollywoodWithDefaultForUnity()` from your code before trying to use the framework.

**Note:** Some systems of the framework like the `ObservableHandler` require that the injection context have a `ILogger` instance and a `IAsserter` instance ready. This is the case when using the [Automatic Setup](#automatic-setup) or when using `Hollywood.Unity.Helper.InitializeHollywoodWithDefaultForUnity()`. Otherwise, be sure that the systems using the framework have access to a `ILogger` and a `IAsserter` higher in their hierarchies.

### Logs and Assert

To enable asserts and logs for the framework internal operations you need to add the following scripting define symbols to your project's player setting:

- HOLLYWOOD_ASSERT
- HOLLYWOOD_LOG

If you used the [Automatic Setup](#automatic-setup) the framework will already use default implementations for its assertions and logs. Otherwise, you will need to assign your own instances to `Hollywood.Log.Logger` and `Hollywood.Assert.Asserter` before creating and setting up your injection context.

#### Log Level

You can switch to different log levels from the `ILogger` instance. If you used the [Automatic Setup](#automatic-setup) or used the `Hollywood.Unity.Helper.InitializeHollywoodWithDefaultForUnity()` method to initialized the framework, you can also specified one of the following scripting define symbols to set the initial log level:

- HOLLYWOOD_UNITY_LOG_TRACE
- HOLLYWOOD_UNITY_LOG_MESSAGE
- HOLLYWOOD_UNITY_LOG_WARNING
- HOLLYWOOD_UNITY_LOG_ERROR
- HOLLYWOOD_UNITY_LOG_FATAL_ERROR
- HOLLYWOOD_UNITY_LOG_NONE

Each of them will activate the corresponding log level and above so there is no use to set multiple of them at the same time.

## Concept

This framework main goal is to help you organize your systems following the inversion of control principle and provide you with tools like C# attributes and interfaces to easily and explicitly create a hierarchy for your systems to live in.

It is a dependency injection framework with a twist: instead of choosing a lifetime type (ie: Transient, Scoped, Singleton) for each system, you instead choose which system or module is owning which other systems, creating a hierarchy of systems.

When a system needs another system for its operations, the injection framework starts from the system needing the other system and goes up in the hierarchy where the system is owned until it finds the needed dependency. This makes sure that a more specific system always has to be lower in the hierarchy and that you don't end up with more generic system having wrongful dependencies to specific systems.

## Usage

### Owns and Needs

Before a system can access any other systems as dependencies, those systems needs to be owned. You can declare what a system owns by adding `[Owns(typeof(T))]` attributes on top of its class, where `T` is the type of a system being owned.

When a system needs to access another system, it can add a `[Needs]` attribute on top of a field of a system's type it needs to access to. This field will then automatically be assigned to the proper instance of the needed system, if the system exists at the same or higher hierarchy where the declaring system is owned.

You can group together owned systems over a class implementing the `IModule` interface. When you do so all systems owned by the module are considered to be at the same hierarchy level as where the module itself is owned, and recursively if your module owns other modules. This lets you organize your systems together per concept but still makes them accessible for systems outside of your module but at a deeper hierarchy level. Note that a module can't have any dependency itself.

### State Machine

The framework contains a state machine class, `StateMachine<TInitialState>`, that you can derive from and use to transition from state to state. This allows you to have systems with a specific and controlled lifetime by having states owning different systems. When you transition from one state to another, all systems owned by the previous states get disposed and the ones from the new state get created.

This is useful when some systems in your application should only exist when the context is appropriate.

### Observers

The framework contains some support types to easily use the observer pattern between your system.

When a system want to be able to send events that can be received by other systems, it can implement the `IObservable<T>` interface which forces it to implement a subscription method to register `IObserver<T>` instances to which it can then keep track of and send events to using the observers instance ``OnReceived(T value)`` method.

To ease the implementation part of the `IObservable<T>` interface, you can make use of an `ObservableHandler<T>` instance which your system can own and need at the same time and forward the subscription called to it. You can then use it to send your events too.

```c#
[Owns(typeof(ObservableHandler<SomeEvent>))]
public class SomeSystem : IObservable<SomeEvent>
{
    [Needs]
    private ObservableHandler<SomeEvent> _observableHandler;

    public IUnsubscriber Subscribe(IObserver<SomeEvent> observer)
    {
        return _observableHandler.Subscribe(observer);
    }

    public void SomeMethod()
    {
        _observableHandler.Send(new SomeEvent());
    }
}
```

This is useful to avoid having wrongful dependencies when a specific system wants to receive events from a generic system without having the generic system having to know about the specific system which is not needed for it to operate. This can also be used to avoid dependency cycle when two system at the same hierarchy level wants to communicate together.

### Item Observers

Where [Observers](#observers) lets you send and received events from systems to systems, the item observer system is used to send events when object gets created and destroyed.

When a system wants to be notified about an instance creation and destruction for a specific type, it can implement the `IItemObserver<T>` interface. Note that it won't received any creation notification for instances that already exist before the system was fully initialized.

When an instance of a specific type wants to notify its creation and destruction it should use the `Injector.Advanced.NotifyItemCreation(...)` and `Injector.Advanced.NotifyItemDestruction(...)` methods when it gets created and then destroyed.

#### ObservedMonoBehaviour

You can use the `[ObservedMonoBehaviour]` attribute on top of a `MonoBehaviour` derived type to automatically notify item creation when the component is awoken and when it is destroyed, without having to manually call the injector.

This is useful to have a way of communication between objects coming from prefabs and scenes and pure C# systems. It also allows to give them access to those systems. That being said you should be careful if you decide to let your MonoBehaviour keep a reference to a system to make sure the lifetime of the MonoBehaviour is shorter than the systems it is using. The ideal solution being to make your systems control, keep track and use your MonoBehaviours and not the opposite.

### IInitializable and IUpdatable

You can use the `IInitializable` interface on a system which will make it implements the `Task Initialize(CancellationToken token)` method. This method will be called automatically by the injection framework and its task will be awaited to make sure that any other systems needing the system will be initialized only when the task completes. You can make a system to not wait for a particular initialization of one of its needed field dependency by setting the `ignoreInitialization` constructor argument of its `[Needs(ignoreInitialization: true)]` attribute to `true`.

You can use the `IUpdatable` interface on a system which will make it implements the `Task Update(CancellationToken token)` method. This method will be called automatically by the injection framework after the system is fully initialized, which means after the `Initialize` task completed if the system is `IInitializable` or else after all its needed systems are initialized. The task return by the `Update` method is not awaited within the framework and can be used to start a long running task for the lifetime of the system.

Methods from both interfaces receive a `CancellationToken` that you should ideally check after each async operation to know if the system has been disposed.

### Other Features

This section focuses on some more advanced features of the framework. What was describe previously on this document should hopefully cover most of your normal use cases.

#### Adding External Instance

There might be cases where you want to be able to use a system through the framework that was not created by it. This can happen when you don't have control over the system's creation or when the system is created before the framework was ready to be used.

When this is the case, you can use `Hollywood.Injector.AddExternalInstance<T>(...)` to add your instance to the framework. Instances added can then be retrieved then from other system as needs dependencies.

#### IgnoreTypeAttribute, IncludeTypeAttribute and InheritsFromInjectableAttribute

Due to technical architecture choices made to improve the time taken to inject compiled assemblies there might be cases where you need to give some hints to the framework about your systems which otherwise might leads to systems not being injected properly.

The `[IgnoreType]` attribute can be used on top of class to make sure the framework doesn't choose this type when resolving a type.

The `[IncludeType]` attribute can be used when you have a class type implementing an interface used within your systems but the class type and the interface lives in different assemblies and the class type doesn't have any owns or needs. This comes from the fact that when injecting an assembly, the framework only consider types and interfaces from within the assembly for which it can deduce if they will be used through injection. It doesn't have access to any details concerning types in other assemblies.

The `[InheritsFromInjectable]` attribute can be used when you have a type inheriting from a type which has its own owns or needs when the base type is not a direct parent of the derived type or when the base type is located in a different assembly. To say it differently, you don't need this attribute when your type directly derives from a type located in the same assembly and which have owns and needs, otherwise you need this attribute. If the base type having owns and needs is not the direct parent of the type you need to specify in the attribute what is the base type having own and needs. Like for `[IncludeType]` attribute, this is due to the fact that when injecting code in the compiled assembly, the framework doesn't know much about types in other assemblies.

#### OwnsAllAttribute

This attribute is very similar to the `[Owns(typeof(T))]` attribute except that it will owns all concrete types that match the `T` type.

#### IResolvable and IDisposable

One of the requirement for this framework was that the kind code injected by the framework post assembly compilation would also be writeable manually. This can be achieved through the `IResolvable` and `IDisposable` interfaces, both called by the framework.

When injecting a system manually, you usually want to add owned instances from the system's constructor and resolves dependencies (which would otherwise come from `[Needs]` field attributes) from the `IResolvable.Resolve()` method.

The `IDisposable.Dispose()` method can be used both when injecting a system automatically or manually and will be called when a system gets disposed.

## Why you should used interfaces for Owns and Needs

You can use both class or interface types for `[Needs]` and `[Owns(typeof(T))]` attributes. The advantages of using interface types though is that you can more easily change the concrete implementation type of your systems. This can be useful for testing, platform specific systems or configuration specific systems.

## How it works

One of the main objective when building this framework was to reduce the amount of reflection calls at runtime to ensure having a fast and allocation friendly system. To achieve such results, the framework analyzes compiled assemblies and looks for uses of interfaces and attributes from the framework in order to modify and inject code inside existing classes. The framework also injects types maps to be able to know relations between classes and interfaces without having to call reflection methods at runtime.

Each owned system from `[Owns(typeof(T))]` attributes on a system type will be converted to an `Injector.AddInstance<T>()` call within the system constructor method. Each `[Needs]` attribute on fields are converted to a `Injector.FindDependency<T>(...)` call from within a resolution method added during code injection.
