using System;
using System.ComponentModel;

namespace Hollywood.Runtime.Internal
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	public class __Hollywood_PostProcessedAttribute : Attribute
	{
		public __Hollywood_PostProcessedAttribute() { }
	}
}
