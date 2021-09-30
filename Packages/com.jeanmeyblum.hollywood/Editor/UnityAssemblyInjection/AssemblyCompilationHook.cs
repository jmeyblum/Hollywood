using Hollywood.Editor.AssemblyInjection;
using Mono.Cecil;
using Mono.Cecil.Pdb;
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
						InjectAssemblyIfIncluded(assembly.outputPath, ignoreAlreadyInjectedException: true);
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

		private static void InjectAssemblyIfIncluded(string assemblyPath, bool ignoreAlreadyInjectedException = false)
		{
			if (!IsIncluded(assemblyPath))
			{
				return;
			}

			Inject(assemblyPath, ignoreAlreadyInjectedException);
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

		public static void Inject(string assemblyPath, bool ignoreAlreadyInjectedException = false)
		{
			var assemblyResolver = new DefaultAssemblyResolver();
			foreach (var path in CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All)
				.Select(p => Path.GetDirectoryName(p))
				.Union(CompilationPipeline.GetAssemblies().Select(a => Path.GetFullPath(Path.GetDirectoryName(a.outputPath))))
				.Where(p => p != null).Distinct())
			{
				assemblyResolver.AddSearchDirectory(path);
			}

			using (var assemblyStream = new FileStream(assemblyPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
			{
				using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyStream, new ReaderParameters(ReadingMode.Immediate) { ReadSymbols = true, ReadWrite = true, AssemblyResolver = assemblyResolver, SymbolReaderProvider = new PdbReaderProvider() }))
				{
					try
					{
						var injectionResult = UnityAssemblyInjector.Inject(assemblyDefinition);

						if (injectionResult == InjectionResult.Modified)
						{
							assemblyDefinition.Write(assemblyStream, new WriterParameters { WriteSymbols = true, SymbolWriterProvider = new PdbWriterProvider() });
						}
					}
					catch (AssemblyAlreadyInjectedException exception)
					{
						if (!ignoreAlreadyInjectedException)
						{
							throw exception;
						}
					}
				}
			}
		}
	}
}