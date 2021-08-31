using System;

namespace Hollywood
{
	/// <summary>
	/// Class attribute to make this class instance owns all types assignable for the specified type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OwnsAllAttribute : Attribute
	{
		private readonly Type _ownedType;

		public OwnsAllAttribute(Type ownedType)
		{
			_ownedType = ownedType;
		}
	}
}