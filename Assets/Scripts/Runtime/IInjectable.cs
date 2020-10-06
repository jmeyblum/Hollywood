using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Hollywood.Runtime.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IInjectable
    {
        void __Resolve();
    }
}