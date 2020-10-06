using BSpace;
using Hollywood.Runtime;
using Hollywood.Runtime.Internal;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
/*
[Owns(typeof(IBInterface2))]
public class AController : MonoBehaviour
{
    [Needs]
    IBInterface _bInterface;
}

[Owns(typeof(IBInterface2))]
public class AControllerFinal : System.IDisposable, IOwner, IInjectable
{
    [Needs]
    IBInterface _bInterface;

    [Needs]
    IBInterface2 _bInterface2;

    private bool __disposedValue;

	public HashSet<object> __ownedInstances { get; set; }

	public AControllerFinal()
    {
        Hollywood.Runtime.Injector.CreateOwnedInstance<IBInterface>(this);
        Hollywood.Runtime.Injector.CreateOwnedInstance<IBInterface2>(this);
    }

    public void __ResolveDependencies()
	{
		_bInterface = Hollywood.Runtime.Injector.ResolveDependency<IBInterface>();
		_bInterface2 = Hollywood.Runtime.Injector.ResolveDependency<IBInterface2>();

        // if root IInjectable :
        Hollywood.Runtime.Injector.ResolveOwnedInstances(this);
        // else base.__ResolveDependencies();
    }

    void System.IDisposable.Dispose()
    {
        if (!__disposedValue)
        {
            Hollywood.Runtime.Injector.DisposeOwnedInstances(this);

            __disposedValue = true;
        }
    }
}


namespace __Hollywood.__Assembly.A
{
    public static class __WarmUpInjectedInterfaces
    {
        public static string[] __interfaces = new string[]
        {
            "IBInterface2",
            "IBInterface",
            "SomeInterface0",
        };
    }
}

*/

public static class TestS
{
    [UnityEditor.MenuItem("Jean/Activate")]
    public static void Activate()
	{
        var context = new Hollywood.Runtime.ReflectionContext();
        var p = Injector.GetInstance<ParentController>(context); //

        var t = Injector.GetInstance<TestController>();
	}
}

[Owns(typeof(IBInterface))]
public class ParentController
{

}

public interface ITestController
{

}

public class SomeB : IBInterface
{
}

[Owns(typeof(IBInterface2))]
public class TestController : ParentAController, ITestController
{
    [Needs]
    IBInterface _myB;

    public List<int> t = new List<int>();
    public int toto = 5;

    //

    /*public TestController(string t) : base(int.Parse(t))
	{
        toto = 12;

        Debug.Log(t);
	}*/
}

public class TestControllerManual : IInjectable
{
	IBInterface _myB;

    private TestControllerManual()
	{
        Hollywood.Runtime.Injector.Advanced.AddInstance<IBInterface2>(this);
	}

    void IInjectable.__Resolve()
	{
        _myB = Hollywood.Runtime.Injector.FindDependency<IBInterface>(this);

        Hollywood.Runtime.Injector.Internal.ResolveOwnedInstances(this);
    }
}