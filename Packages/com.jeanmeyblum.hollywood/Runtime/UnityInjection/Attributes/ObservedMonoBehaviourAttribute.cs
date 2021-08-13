using System;

namespace Hollywood.Runtime.UnityInjection
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ObservedMonoBehaviourAttribute : Attribute
    {	
		public ObservedMonoBehaviourAttribute() { }
	}
}
