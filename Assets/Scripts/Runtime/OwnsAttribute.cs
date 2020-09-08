using System;

namespace Hollywood.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class OwnsAttribute : Attribute
    {
        private Type _interfaceType;

        public OwnsAttribute(Type interfaceType)
        {
            _interfaceType = interfaceType;
        }
    }
}