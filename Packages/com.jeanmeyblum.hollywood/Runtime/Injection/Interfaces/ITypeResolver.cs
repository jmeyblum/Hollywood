using System;
using System.Collections.Generic;

namespace Hollywood
{
	/// <summary>
	/// Responsible for getting type information.
	/// </summary>
	public interface ITypeResolver
	{
		/// <summary>
		/// Retrieves a type to instantiate for a given type T.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		Type Get<T>();

		/// <summary>
		/// Retrieves types to instantiate for a given type T.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		IEnumerable<Type> GetAll<T>();

		/// <summary>
		/// Retrieves assignable types for a given type.
		/// This only works for types known by the injection system.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		IEnumerable<Type> GetAssignableTypes(Type type);
		void Reset();
	}
}
