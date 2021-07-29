using Hollywood.Runtime;
using Hollywood.Runtime.Internal;

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

////[Owns(typeof(ISomethingOwnedByA))]
//public class A : IInjected
//{

//	//[Needs]
//	ISomethingForA _somethingForA;

//	void IInjected.__Resolve()
//	{
//		___Resolve___();
//	}

//	protected virtual void ___Resolve___()
//	{
//		_somethingForA = Hollywood.Runtime.Injector.FindDependency<ISomethingForA>(this);
//	}
//}