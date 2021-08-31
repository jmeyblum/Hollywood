using System;

namespace Hollywood
{
	/// <summary>
	/// Field attribute used to automatically resolve a dependency to a system existing at the same level or higher in the hierarchy 
	/// where this enclosing system instance is owned.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class NeedsAttribute : Attribute
	{
		private readonly bool _ignoreInitialization;

		public NeedsAttribute()
		{ }

		public NeedsAttribute(bool ignoreInitialization)
		{
			_ignoreInitialization = ignoreInitialization;
		}
	}
}
