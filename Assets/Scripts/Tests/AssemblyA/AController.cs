using BSpace;
using Hollywood.Runtime;
using Hollywood.Runtime.Internal;
using System;
using System.Collections;
using System.Collections.Generic;

public class Patate
{
    public void Test()
	{
        int i = 0;

        FormattableString s = $"Test: {((Func<int>)(()=> { ++i; return i; })).Invoke()}";
         
        Console.WriteLine(s);
        Console.WriteLine(s);
	} 
}

public static class TestS
{
    [UnityEditor.MenuItem("Jean/Activate")]
    public static void Activate()
	{
        Hollywood.Runtime.UnityInjection.Helper.InitializeHollywoodWithDefaultForUnity();

        var p = Injector.GetInstance<TestModule>(); //

        var t = Injector.GetInstance<ITestController>(p);

        var ts = Injector.GetInstances<IModule>();

        Injector.DisposeInstance(p);
	}
}
 
public interface ITestModule : IModule
{ }

[Owns(typeof(IBInterface))]
[Owns(typeof(IParent))]
[Owns(typeof(ITestController))]
public class TestModule : ITestModule
{

}

public interface IBaseModule : IModule
{ }

[Owns(typeof(ITestModule))]
[Owns(typeof(IOtherSystem))] 
public class BaseModule : IBaseModule
{

}

public interface IOtherSystem
{ }

[Owns(typeof(ISubOtherSystem))]
public class OtherSystem : IOtherSystem
{ }

public interface ISubOtherSystem
{ }

public class SubOtherSystem : ISubOtherSystem
{
    [Needs] 
    ITestController TestController;
}

public interface IParent
{

}

public class ParentController : IParent, IDisposable, IResolvable
{
    int toto = 42;

    private IManualOwned m;

	public ParentController()
	{
        m = Injector.Advanced.AddInstance<IManualOwned>(this);

		toto = 12;
	}

	public void Dispose()
	{
        Injector.DisposeInstance(m);
	}

	public void Resolve()
	{
        Injector.Advanced.ResolveInstance(m);
	}
}

public interface IManualOwned
{

}

public class ManualOwned : IManualOwned
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

public interface IInjectedParent
{
    void Resolve();
}

public class TestControllerParentManual : __Hollywood_Injected, IInjectedParent
{
    //[Needs]
    ISubOtherSystem _subOtherSystem;

	void __Hollywood_Injected.__Resolve()
	{
        //_subOtherSystem = Hollywood.Runtime.Injector.FindDependency<ISubOtherSystem>(this);

        //Hollywood.Runtime.Injector.Internal.ResolveOwnedInstances(this);
         
        A_IInjected_Resolve_();
    }
     
    protected virtual void YoYo()
	{

	}

	void IInjectedParent.Resolve()
	{
        ((__Hollywood_Injected)this).__Resolve();
	}

    protected void A_IInjected_Resolve_() // >__Hollywood_Injected<>Resolve<
    {
        _subOtherSystem = Hollywood.Runtime.Injector.FindDependency<ISubOtherSystem>(this);

        Hollywood.Runtime.Injector.Internal.ResolveOwnedInstances(this);
    }
}



//[Owns(typeof(IBInterface2))]
public class TestControllerManual : TestControllerParentManual, __Hollywood_Injected
{
    //[Needs]
    IBInterface _myB;
    IBInterface2 _myB1;

    private TestControllerManual()
	{
        Hollywood.Runtime.Injector.Advanced.AddInstance<IBInterface2>(this);
	}

	void __Hollywood_Injected.__Resolve()
	{
        B_IInjected_Resolve_();
        //Hollywood.Runtime.Injector.Internal.ResolveOwnedInstances(this);
    }

    protected void B_IInjected_Resolve_() // >__Hollywood_Injected<>Resolve<
    {
        _myB = Hollywood.Runtime.Injector.FindDependency<IBInterface>(this);
        _myB1 = Hollywood.Runtime.Injector.FindDependency<IBInterface2>(this);

        A_IInjected_Resolve_();
    }
}