using System.Collections.Generic;
using UnityEngine;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	public class ProjectSettings : ScriptableObject
	{
		public List<AssemblyInclusionRule> AssemblyInclusionRules = new List<AssemblyInclusionRule>();
	}
}