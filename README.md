# Hollywood

Hollywood is a dependency injection framework primarily made for Unity.

## Installation

Modify the `manifest.json` file of your Unity project under the Packages folder and add a the dependency to the package:

```json
{
  "dependencies": {
    ...
    "com.jeanmeyblum.hollywood": "git+https://github.com/jmeyblum/Hollywood?path=Packages/com.jeanmeyblum.hollywood",
    ...
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

## Usage

### Owns and Needs

### State Machine

### Observers

### Item Observers

### IInitializable and IUpdatable
