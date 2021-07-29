using System.ComponentModel;

namespace Hollywood.Runtime.Internal
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IInjected
	{
		void __Resolve();
	}
}