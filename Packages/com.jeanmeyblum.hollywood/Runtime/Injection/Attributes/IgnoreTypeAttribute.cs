using System;

namespace Hollywood.Runtime
{
	/// <summary>
	/// Class attribute to avoid the default TypeResolver from using the type this attribute is used on.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class IgnoreTypeAttribute : Attribute
	{

	}
}