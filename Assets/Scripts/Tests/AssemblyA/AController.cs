using BSpace;
using Hollywood.Runtime;
using Hollywood.Runtime.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

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
        var context = new Hollywood.Runtime.ReflectionContext();
        var p = Injector.GetInstance<IParent>(context); //

        var t = Injector.GetInstance<ITestController>(context, p);
	}
}

public interface IParent
{

}

[Owns(typeof(IBInterface))]
public class ParentController : IParent
{
    int toto = 42;

	public ParentController()
	{
        Injector.Advanced.AddInstance<IManualOwned>(this, new Hollywood.Runtime.ReflectionContext());

		toto = 12;
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

//[Owns(typeof(IBInterface2))]
public class TestControllerManual : IInjected
{
    //[Needs]
    IBInterface _myB;
    IBInterface2 _myB1;

    private TestControllerManual()
	{
        Hollywood.Runtime.Injector.Advanced.AddInstance<IBInterface2>(this);
	}

	void IInjected.__Dispose()
	{
        Hollywood.Runtime.Injector.DisposeInstance(this);
	}

	void IInjected.__Resolve()
	{
        _myB = Hollywood.Runtime.Injector.FindDependency<IBInterface>(this);
        _myB1 = Hollywood.Runtime.Injector.FindDependency<IBInterface2>(this);

        Hollywood.Runtime.Injector.Internal.ResolveOwnedInstances(this);
    }
}