using Hollywood.Runtime;
using Hollywood.Runtime.Internal;

public static class TestSomeC
{
    [UnityEditor.MenuItem("Jean/Test Inheritance")]
    public static void Activate()
    {
        Hollywood.Runtime.UnityInjection.Helper.InitializeHollywoodWithDefaultForUnity();

        var m = Injector.GetInstance<ITestCModule>(); //

        var c = Injector.GetInstance<IC>(m);

        Injector.DisposeInstance(m);
    }
}

public interface ITestCModule
{

}
 
[Owns(typeof(ISomethingForC))]
[Owns(typeof(ISomethingForA))]
[Owns(typeof(IC))]
public class TestCModule : ITestCModule 
{

}

interface ISomethingForC 
{

}

public class SomethingForC : ISomethingForC
{

}

interface ISomethingOwnedByC { }

public class SomethingOwnedByC : ISomethingOwnedByC
{

}

public interface IC { }

[InheritsFromInjectable(typeof(A))] 
[Owns(typeof(ISomethingOwnedByC))]
public class C : B, IC
{
	[Needs]
	ISomethingForC _somethingForC;
}

//[InheritsFromInjectable(typeof(A))] 
//public class C : B, __Hollywood_Injected
//{
//	//[Needs]
//	ISomethingForC _somethingForC;

//	void __Hollywood_Injected.__Resolve()
//	{
//		___Resolve___();
//	}

//	protected override void ___Resolve___()
//	{
//		_somethingForC = Hollywood.Runtime.Injector.FindDependency<ISomethingForC>(this);

//		base.___Resolve___();
//	}
//}