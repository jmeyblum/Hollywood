using System;

namespace Hollywood.Runtime
{
	[AttributeUsage(AttributeTargets.Field)]
	public class NeedsAttribute : Attribute
	{
		private readonly bool _ignoreInitialization;

		public NeedsAttribute()
		{}

		public NeedsAttribute(bool ignoreInitialization)
		{
			_ignoreInitialization = ignoreInitialization;
		}
	}
}
