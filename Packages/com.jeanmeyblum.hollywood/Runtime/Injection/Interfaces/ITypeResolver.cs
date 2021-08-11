using System;
using System.Collections.Generic;

namespace Hollywood.Runtime
{
	public interface ITypeResolver
	{
		Type Get<T>();
		IEnumerable<Type> GetAll<T>();
		IEnumerable<Type> GetAssignableTypes(Type type);
		void Reset();
	}
}
