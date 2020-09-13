using Mono.Cecil;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace Hollywood.Editor
{

	public static class PostAssemblyCompilationProcessor
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

			if (assemblyPath.Contains("Unity"))
			{
				return;
			}

			// TODO -jmeyblum: remove later
			if (!assemblyPath.Contains("Assembly.A"))
			{
				return;
			}

			Inject(assemblyPath);
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