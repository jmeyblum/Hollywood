using System.Collections.Generic;
using System.ComponentModel;

namespace Hollywood.Runtime.Internal
{
	[EditorBrowsable(EditorBrowsableState.Never)]
    public interface IOwner
    {
        HashSet<object> __ownedInstances { get; set; }
    }
}