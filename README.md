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

### Automatic Setup

Simply add the following scripting define symbol to your project's player setting:

- HOLLYWOOD_UNITY_AUTO_SETUP

This will create a new ­­­­­`Injection Context` each time you enter play mode which already contains a `ILogger` and a `IAsserter` insance which are set also as the logger and asserter for the framework operations.

### Manual Setup

Before your application try to use the framework, you need to assign the static injection context instance to `Hollywood.Runtime.Injector.InjectionContext`. You can do so yourself or make use of the helper methods find in `Hollywood.Runtime.Helper` and `Hollywood.Runtime.UnityInjection.Helper`.

The easiest way is to call `Hollywood.Runtime.UnityInjection.Helper.InitializeHollywoodWithDefaultForUnity()` from your code before trying to use the framework.

**Note:** Some systems of the framework like the `ObservableHandler` require that the injection context have a `ILogger` instance and a `IAsserter` instance ready. This is the case when using the [Automatic Setup](#automatic-setup) or when using `Hollywood.Runtime.UnityInjection.Helper.InitializeHollywoodWithDefaultForUnity()`. Otherwise, be sure that the systems using the framework have access to a `ILogger` and a `IAsserter` higher in their hierarchies.

### Logs and Assert

To enable asserts and logs for the framework internal operations you need to add the following scripting define symbols to your project's player setting:

- HOLLYWOOD_ASSERT
- HOLLYWOOD_LOG

If you used the [Automatic Setup](#automatic-setup) the framework will already use default implementations for its assertions and logs. Otherwise, you will need to assign your own instances to `Hollywood.Runtime.Log.Logger` and `Hollywood.Runtime.Assert.Asserter` before creating and setting up your injection context.

## Concept

This framework main goal is to help you organize your systems following the inversion of control principle and provide you with tools like C# attributes and interfaces to easily and explicitly create a hierachy for your systems to live in.

It is a dependency injection framework with a twist: instead of choosing a lifetime type (ie: Transient, Scoped, Singleton) for each system, you instead choose which system or module is owning which other systems, creating a hierarchy of systems.

When a system needs another system for its operations, the injection framework starts from the system needing the other system and goes up in the hierarchy where the system is owned until it finds the needed dependency. This makes sure that a more specific system always has to be lower in the hierarchy and that you don't end up with more generic system having wrongful dependencies to specific systems.

## Usage

### Owns and Needs

Before a system can access any other systems as dependencies, those systems needs to be owned. You can declare what a system owns by adding `[Owns(typeof(T))]` attributes on top of its class, where `T` is the type of a system beeing owned.

When a system needs to access another system, it can add a `[Needs]` attribute on top of a field of a system's type it needs to access to. This field will then automatically be assigned to the proper instance of the needed system, if the system exists at the same or higher hierarchy where the declaring system is owned.

You can group together owned systems over a class implementing the `IModule` interface. When you do so all systems owned by the module are considered to be at the same hierarchy level as where the module itseld is owned, and recursively if your module owns other modules. This lets you organize your systems together per concept but still make them accessible for system outside of your module but at a deeper hierarchy level. Note that a module can't have any dependency itself.

### State Machine

The framework contains a state machine class, `StateMachine<TInitialState>`, that you can derive from and use to transition from state to state. This lets you to have systems with a specific and controlled lifetime by having states owning different systems. When you transition from one state to another, all systems owned by the previous states get disposed and the ones from the new state get created.

This is useful when some systems in your application should only exist when the context is appropriate.

### Observers

The framework contains some support types to easily use the observer pattern between your system.

When a system want to be able to send events that can be received by other systems, it can implement the `IObservable<T>` interface which forces it to implement a subscription method to register `IObserver<T>` instances to which it can then keep track of and send events to using the observers instance ``OnReceived(T value)`` method.

To ease the implementation part of the `IObservable<T>` interface, you can make use of an `ObservableHandler<T>` instance which your system can own and need at the same time and forward the subscription called to it. You can then use it to send your events too.

```c#
[Owns(typeof(ObservableHandler<SomeEvent>))]
public class SomeSystem
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

When a system wants to be notified about an instance creation and destruction for a specific type, it can implement the `IItemObserver<T>` interface. Note that it won't received any creation notification for instances that already exist before the system was initialized.

When an instance of a specific type wants to notify its creation and destruction it should use the `Injector.Advanced.NotifyItemCreation` and `Injector.Advanced.NotifyItemDestruction` when it gets created and then destroyed.

#### ObservedMonoBehaviour

You can use the `[ObservedMonoBehaviour]` attribute on top of a `MonoBehaviour` derived type to automatically notify item creation when the component is awoken and when it is destroyed, without having to manually call the injector.

### IInitializable and IUpdatable

### Other Features

## How it works
