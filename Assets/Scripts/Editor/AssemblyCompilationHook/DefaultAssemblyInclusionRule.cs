using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Hollywood.Editor.AssemblyCompilationHook
{
	[CreateAssetMenu(menuName = "Hollywood/DefaultAssemblyInclusionRule")]
	public class DefaultAssemblyInclusionRule : AssemblyInclusionRule
	{
		private static string[] AssemblyExclusionPrefixes = new string[]
		{
			"Unity.",
			"UnityEngine.",
			"UnityEditor.",
			"Hollywood."
		};

		public override bool IsIncluded(string assemblyPath)
		{
			return !DoesAssemblyNameStartsWithDefaultExclusionPrefixes(assemblyPath);
		}

		public static bool DoesAssemblyNameStartsWithDefaultExclusionPrefixes(string assemblyPath)
		{
			if (string.IsNullOrWhiteSpace(assemblyPath))
			{
				return false;
			}

			var assemblyName = Path.GetFileName(assemblyPath);

			return AssemblyExclusionPrefixes.Any(prefix => assemblyName.StartsWith(prefix, System.StringComparison.Ordinal));
		}
	}
}
