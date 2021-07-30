using Hollywood.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class TestCase
{
    [UnityEditor.MenuItem("Jean/Activate")]
    public static void Activate()
	{
        Injector.Dispose();
        Hollywood.Runtime.UnityInjection.Helper.InitializeHollywoodWithDefaultForUnity();

        var p = Injector.GetInstance<ApplicationModule>();

        //Injector.DisposeInstance(p);
	}
}

[Owns(typeof(PlayerModule))]
[Owns(typeof(GameplayModule))]
[Owns(typeof(AnalyticModule))]
[Owns(typeof(LobbySystem))]
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