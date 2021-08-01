using Hollywood.Runtime;
using Hollywood.Runtime.StateMachine;
using System;
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

public class BootSystem : IUpdatable
{
    [Needs]
    private ApplicationStateMachine _stateMachine;

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

public sealed class ApplicationStateMachine : StateMachine<BootState> { }

[Owns(typeof(PlayerModule))]
[Owns(typeof(GameplayModule))]
[Owns(typeof(AnalyticModule))]
[Owns(typeof(ApplicationStateMachine))]
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

public class AnalyticSystem
{
    [Needs]
    private PlayerPreferencesSystem _playerPreferencesSystem;

    [Needs]
    private AnalyticDispatcher _analyticDispatcher;
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