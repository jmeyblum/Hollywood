using UnityEngine;

namespace Hollywood.Editor.AssemblyCompilationHook
{
    public abstract class AssemblyInclusionRule : ScriptableObject
    {
        public abstract bool IsIncluded(string assemblyPath);
    }
}