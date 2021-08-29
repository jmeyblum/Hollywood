using System;

namespace Hollywood.Runtime
{
	/// <summary>
	/// Class attribute to hint the injection to resolve parents dependencies.
	/// Note: Only use on class that inherits from a class using dependency injection.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class InheritsFromInjectableAttribute : IncludeTypeAttribute
	{
		private readonly Type _baseType;

		public InheritsFromInjectableAttribute(Type baseType)
		{
			_baseType = baseType;
		}

		public InheritsFromInjectableAttribute()
		{ }
	}
}