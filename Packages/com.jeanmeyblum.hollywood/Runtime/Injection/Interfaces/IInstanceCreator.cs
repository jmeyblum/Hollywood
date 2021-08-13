using System;

namespace Hollywood.Runtime
{
	/// <summary>
	/// Responsible for creating an object for the specified type.
	/// The type is resolved through the ITypeResolver.
	/// </summary>
	public interface IInstanceCreator
	{
		object Create(Type type);
		void Reset();
	}
}