using System;

namespace Hollywood.Runtime.UnityInjection
{
    /// <summary>
    /// Class attribute that automatically notify the injection system for item creation and destruction
    /// during the Awake and OnDestroy moments of a MonoBehaviour.
    /// If the MonoBehaviour doesn't have an Awake or an OnDestroy method, they will be created automatically
    /// as new public methods in the compiled assembly. Beware that this can lead to method hiding if base or 
    /// derived classes of this class implement their own Awake or OnDestroy methods. If that is the case, explicitly
    /// create the Awake or OnDestroy methods as either virtual or override depending on your use case.
    /// If the methods already exists, the injection system will injects itself at the end of existing methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ObservedMonoBehaviourAttribute : Attribute
    {	
		public ObservedMonoBehaviourAttribute() { }
	}
}
