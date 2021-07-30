using System;

namespace Hollywood.Runtime
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OwnsAllAttribute : Attribute
	{
		private readonly Type _interfaceType;

		public OwnsAllAttribute(Type interfaceType)
		{
			_interfaceType = interfaceType;
		}
	}
}