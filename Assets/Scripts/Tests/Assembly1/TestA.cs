using Hollywood.Runtime;
using Hollywood.Runtime.Internal;
using System;

public interface ISomethingForA
{

}

public class SomethingForA : ISomethingForA
{ 

}

interface ISomethingOwnedByA { }

public class SomethingOwnedByA : ISomethingOwnedByA
{

}

[Owns(typeof(ISomethingOwnedByA))]
public class A
{
	[Needs]
	ISomethingForA somethingForA;
}

public static class __InjectedInterfaces
{
    public static Type[] __interfaceNames = new Type[2]
    {
        typeof(ISomethingForA),
        typeof(ISomethingOwnedByA)
    };
}

////[Owns(typeof(ISomethingOwnedByA))]
//public class A : __Hollywood_Injected
//{

//	//[Needs]
//	ISomethingForA _somethingForA;

//	void __Hollywood_Injected.__Resolve()
//	{
//		___Resolve___();
//	}

//	protected virtual void ___Resolve___()
//	{
//		_somethingForA = Hollywood.Runtime.Injector.FindDependency<ISomethingForA>(this);
//	}
//}