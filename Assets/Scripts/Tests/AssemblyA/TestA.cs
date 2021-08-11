using Hollywood.Runtime;
using Hollywood.Runtime.Observer;
using Hollywood.Runtime.StateMachine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class Application
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootApplication()
	{
        Injector.GetInstance<ApplicationModule>();
    }
}

public class SomeItem : System.IDisposable
{
    public SomeItem()
	{
        Injector.Advanced.NotifyItemCreation(this);
	}

	public void Dispose()
	{
        Injector.Advanced.NotifyItemDestruction(this);
    }
}

public class SomeOtherItem
{

}

public class ExampleItemObserver : IItemObserver<SomeItem>, IItemObserver<SomeOtherItem>
{
	void IItemObserver<SomeItem>.OnItemCreated(SomeItem item)
	{
        UnityEngine.Debug.Log($"{nameof(SomeItem)} created");
	}

	void IItemObserver<SomeOtherItem>.OnItemCreated(SomeOtherItem item)
	{
        UnityEngine.Debug.Log($"{nameof(SomeOtherItem)} created");
    }

	void IItemObserver<SomeItem>.OnItemDestroyed(SomeItem item)
	{
        UnityEngine.Debug.Log($"{nameof(SomeItem)} destroyed");
    }

	void IItemObserver<SomeOtherItem>.OnItemDestroyed(SomeOtherItem item) 
	{
        UnityEngine.Debug.Log($"{nameof(SomeOtherItem)} destroyed");
    }
}

public class BootSystem : IUpdatable, IInitializable, System.IDisposable
{
    [Needs]
    private ApplicationStateMachine _stateMachine;

    [Needs]
    private ExampleItemObserver _exampleItemObserver;

    private SomeItem _item;

	public void Dispose()
	{
        _item.Dispose();
	}

	public Task Initialize(CancellationToken token)
	{
        _item = new SomeItem();

        return Task.CompletedTask;
	}

	Task IUpdatable.Update(CancellationToken token)
	{
        if (Input.GetKeyDown(KeyCode.A))
        {
            _stateMachine.TransitionTo<LobbyState>();
        }

        return Task.CompletedTask;
    }
}

[Owns(typeof(BootSystem))]
public class BootState : IState
{

}

[Owns(typeof(LobbySystem))]
public class LobbyState : IState
{

}

public class AnalyticEvent
{
    public string Message;
}

[InheritsFromInjectable]
public sealed class ApplicationStateMachine : StateMachine<BootState>, IObserver<AnalyticEvent>
{
    [Needs]
    private Hollywood.Runtime.ILogger logger;

	public override Task Initialize(CancellationToken token)
	{
		return base.Initialize(token);
	}

	public void OnReceived(AnalyticEvent value)
	{
        logger.LogMessage($"from analytic: {value.Message}");
	}
}

[Owns(typeof(PlayerModule))]
[Owns(typeof(GameplayModule))]
[Owns(typeof(AnalyticModule))]
[Owns(typeof(ApplicationStateMachine))]
[Owns(typeof(ExampleItemObserver))]
public class ApplicationModule : IModule
{

}

[Owns(typeof(AnalyticSystem))]
[Owns(typeof(AnalyticDispatcher))]
[Owns(typeof(AnalyticThirdPartySystem))]
public class AnalyticModule : IModule
{

}

[Owns(typeof(PlayerSystem))]
[Owns(typeof(PlayerPreferencesSystem))]
public class PlayerModule : IModule
{

}

[Owns(typeof(InventorySystem))]
[Owns(typeof(CharacterSystem))]
[Owns(typeof(PlayerCharacterSystem))]
public class GameplayModule : IModule
{

}

public class CharacterSystem : IInitializable
{
    [Needs]
    private InventorySystem _inventorySystem;

	async Task IInitializable.Initialize(CancellationToken token)
	{
        UnityEngine.Debug.Log($"{nameof(CharacterSystem)} - pre");

        await Task.Delay(1000);

        UnityEngine.Debug.Log($"{nameof(CharacterSystem)} - post");
    }
}

public class InventorySystem : IInitializable
{
    [Needs(ignoreInitialization: true)]
    private PlayerCharacterSystem _playerCharacterSystem; 

    async Task IInitializable.Initialize(CancellationToken token)
	{
        UnityEngine.Debug.Log($"{nameof(InventorySystem)} - pre");

        await Task.Delay(1001);

        UnityEngine.Debug.Log($"{nameof(InventorySystem)} - post");
    }
}

public class PlayerSystem
{
    [Needs]
    private PlayerPreferencesSystem _playerPreferencesSystem;
}

public class PlayerCharacterSystem : IInitializable
{
    [Needs]
    private PlayerSystem _playerSystem;

    [Needs]
    private CharacterSystem characterSystem;

    public bool Init = false;

    Task IInitializable.Initialize(CancellationToken token)
    {
        Init = true;

        return Task.CompletedTask;
	}
}

public class PlayerPreferencesSystem : IInitializable
{
	async Task IInitializable.Initialize(CancellationToken token)
	{
        UnityEngine.Debug.Log($"{nameof(PlayerPreferencesSystem)} - pre");

        await Task.Delay(500);

        UnityEngine.Debug.Log($"{nameof(PlayerPreferencesSystem)} - post");
    }
}

public class AnalyticThirdPartySystem : IInitializable
{
	async Task IInitializable.Initialize(CancellationToken token)
	{
        UnityEngine.Debug.Log($"{nameof(AnalyticThirdPartySystem)} - pre");

        await Task.Delay(500);

        UnityEngine.Debug.Log($"{nameof(AnalyticThirdPartySystem)} - post");
    }
}

public class AnalyticDispatcher
{
    [Needs] 
    private AnalyticThirdPartySystem _analyticThirdPartySystem;
}

[Owns(typeof(ObservableHandler<AnalyticEvent>))]
public class AnalyticSystem : IObserver<StateMachineEvent>, IInitializable, IObservable<AnalyticEvent>
{
    [Needs]
    private PlayerPreferencesSystem _playerPreferencesSystem;

    [Needs]
    private AnalyticDispatcher _analyticDispatcher;

    [Needs]
    private Hollywood.Runtime.ILogger _logger;

    [Needs(ignoreInitialization: true)]
    private ApplicationStateMachine _applicationStateMachine;

    [Needs]
    private ObservableHandler<AnalyticEvent> ObservableHandler;

	public IUnsubscriber Subscribe(IObserver<AnalyticEvent> observer)
	{
        return ObservableHandler.Subscribe(observer);        
    }

	Task IInitializable.Initialize(CancellationToken token)
	{
        _applicationStateMachine.Subscribe(this);

        Subscribe(_applicationStateMachine);

        return Task.CompletedTask; 
    }

	void IObserver<StateMachineEvent>.OnReceived(StateMachineEvent value)
	{
        _logger.LogMessage($"Sending analytic: {value.Type}");

        ObservableHandler.Send(new AnalyticEvent() { Message = "Hello from the analytics!" });
    }
}

public class LobbySystem : IInitializable
{
    [Needs]
    private PlayerCharacterSystem _playerCharacterSystem;

    [Needs]
    private AnalyticSystem _analyticSystem;

    async Task IInitializable.Initialize(CancellationToken token)
	{
        UnityEngine.Debug.Log($"{nameof(LobbySystem)} - pre");

        await Task.Delay(1000);

        UnityEngine.Debug.Log($"{nameof(LobbySystem)} - post");
    }
}