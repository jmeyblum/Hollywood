using UnityEngine;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	public abstract class AssemblyInclusionRule : ScriptableObject
	{
		public abstract bool IsIncluded(string assemblyPath);
	}
}