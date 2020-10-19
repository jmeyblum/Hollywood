﻿using Hollywood.Editor.AssemblyInjection;
using Mono.Cecil;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	internal static class AssemblyCompilationHook
	{
		private const string AssemblyInjectedOnceSessionKey = "Hollywood_Assembly_Injected_Once";

		[InitializeOnLoadMethod]
		public static void OnInitializeOnLoad()
		{
			CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;
			CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;

			TryInjectAllAssembliesOnce();
		}

		private static void TryInjectAllAssembliesOnce()
		{
			if (!SessionState.GetBool(AssemblyInjectedOnceSessionKey, false))
			{
				SessionState.SetBool(AssemblyInjectedOnceSessionKey, true);

				foreach (var assembly in CompilationPipeline.GetAssemblies())
				{
					if (File.Exists(assembly.outputPath))
					{
						InjectAssemblyIfIncluded(assembly.outputPath);
					}
				}

				EditorUtility.RequestScriptReload();
			}
		}

		private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
		{
			if (compilerMessages.Any(msg => msg.type == CompilerMessageType.Error) == true)
			{
				return;
			}

			InjectAssemblyIfIncluded(assemblyPath);
		}

		private static void InjectAssemblyIfIncluded(string assemblyPath)
		{
			if (!IsIncluded(assemblyPath))
			{
				return;
			}

			Inject(assemblyPath);
		}

		public static bool IsIncluded(string assemblyPath)
		{
			var projectSettings = ProjectSettingsProvider.TryLoadProjectSettings();

			if (!projectSettings)
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

				if (injectionResult == InjectionResult.Modified)
				{
					assemblyDefinition.Write(new WriterParameters { WriteSymbols = true });
				}
			}
		}
	}
}