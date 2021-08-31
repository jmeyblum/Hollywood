using System;

namespace Hollywood
{
	/// <summary>
	/// Class attribute to make this class instance owns an instance of the specified type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OwnsAttribute : Attribute
	{
		private Type _interfaceType;

		public OwnsAttribute(Type interfaceType)
		{
			_interfaceType = interfaceType;
		}
	}
}