using Hollywood.Runtime;
using Hollywood.Runtime.UnityInjection;
using UnityEngine;

[ObservedMonoBehaviour]
public class SomeObservedComponent : MonoBehaviour
{
	ExampleItemObserver _exampleItemObserver; 

	public void Initialize(ExampleItemObserver exampleItemObserver)
	{
		_exampleItemObserver = exampleItemObserver;
	}
}
