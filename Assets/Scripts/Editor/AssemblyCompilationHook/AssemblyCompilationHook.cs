using Hollywood.Editor.AssemblyInjection;
using Mono.Cecil;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace Hollywood.Editor.AssemblyCompilationHook
{
	internal static class AssemblyCompilationHook
	{
		[InitializeOnLoadMethod]
		public static void OnInitializeOnLoad()
		{
			CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;
			CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
		}

		private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
		{		
			if (compilerMessages.Any(msg => msg.type == CompilerMessageType.Error) == true)
			{
				return;
			}

			if(!IsIncluded(assemblyPath))
			{
				return;
			}

			UnityEngine.Debug.Log(assemblyPath);

			Inject(assemblyPath);
		}

		public static bool IsIncluded(string assemblyPath)
		{
			var projectSettings = ProjectSettingsProvider.TryLoadProjectSettings();

			if(!projectSettings)
			{
				return !DefaultAssemblyInclusionRule.DoesAssemblyNameStartsWithDefaultExclusionPrefixes(assemblyPath);
			}
			else
			{
				return projectSettings.AssemblyInclusionRules?.Any(rule => !rule.IsIncluded(assemblyPath)) != true;
			}
		}

		public static void Inject(string assemblyPath)
		{
			using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters(ReadingMode.Immediate) { ReadSymbols = true, ReadWrite = true, AssemblyResolver = new DefaultAssemblyResolver() }))
			{
				var injectionResult = AssemblyInjector.Inject(assemblyDefinition);

				if(injectionResult == InjectionResult.Modified)
				{
					assemblyDefinition.Write(new WriterParameters { WriteSymbols = true });
				}
			}
		}
	}
}