using System;

namespace Hollywood.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class OwnsAllAttribute : Attribute
    {
        private Type _interfaceType;

        public OwnsAllAttribute(Type interfaceType)
        {
            _interfaceType = interfaceType;
        }
    }
}