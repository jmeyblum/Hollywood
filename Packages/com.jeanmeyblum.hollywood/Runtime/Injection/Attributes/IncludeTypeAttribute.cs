using System;

namespace Hollywood
{
	/// <summary>
	/// Class attribute to force the default TypeResolver to know the type this attribute is used on.
	/// Note: This is only necessary for a type that would implement an injectable interface from another assembly and which doesn't own or need any other type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class IncludeTypeAttribute : Attribute
	{

	}
}