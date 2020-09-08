using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hollywood.Runtime
{
    public interface IContext
    {
        Type Get<T>();
        IEnumerable<Type> GetAll<T>();
    }
}
