using Hollywood.Editor.AssemblyInjection;
using Hollywood.Runtime;
using Hollywood.Runtime.UnityInjection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	internal class UnityAssemblyInjector : AssemblyInjector
	{
		internal static readonly Type ObservedMonoBehaviourAttributeType = typeof(ObservedMonoBehaviourAttribute);

		private static readonly System.Reflection.MethodInfo NotifyItemCreationMethod = typeof(Injector.Advanced).GetMethod(nameof(Injector.Advanced.NotifyItemCreation), StaticBindingFlags);
		private static readonly System.Reflection.MethodInfo NotifyItemDestructionMethod = typeof(Injector.Advanced).GetMethod(nameof(Injector.Advanced.NotifyItemDestruction), StaticBindingFlags);

		private MethodReference NotifyItemCreationMethodReference;
		private MethodReference NotifyItemDestructionMethodReference;

		protected UnityAssemblyInjector(AssemblyDefinition assemblyDefinition) 
			: base(assemblyDefinition)
		{}

		protected override void Inject()
		{
			NotifyItemCreationMethodReference = AssemblyDefinition.MainModule.ImportReference(NotifyItemCreationMethod);
			NotifyItemDestructionMethodReference = AssemblyDefinition.MainModule.ImportReference(NotifyItemDestructionMethod);

			var injectionData = new UnityInjectionData(AssemblyDefinition.MainModule);

			Inject(injectionData as InjectionData);
			Inject(injectionData);
		}

		protected void Inject(UnityInjectionData injectionData)
		{
			if (injectionData.InjectableMonoBehaviours.Count() == 0)
			{
				return;
			}

			Result = InjectionResult.Modified;

			foreach (var injectableMonoBehaviour in injectionData.InjectableMonoBehaviours)
			{
				Inject(injectableMonoBehaviour);
			}
		}

		private void Inject(InjectableMonoBehaviour injectableMonoBehaviour)
		{
			InjectMethodCallInMethod(injectableMonoBehaviour, "Awake", NotifyItemCreationMethodReference);
			InjectMethodCallInMethod(injectableMonoBehaviour, "OnDestroy", NotifyItemDestructionMethodReference);
		}

		private void InjectMethodCallInMethod(InjectableMonoBehaviour injectableMonoBehaviour, string methodName, MethodReference methodReference)
		{
			if (!(injectableMonoBehaviour.Type.Methods.FirstOrDefault(m => m.Name == methodName && m.HasThis && m.ReturnType == VoidType) is MethodDefinition method))
			{
				method = new MethodDefinition(
				methodName,
				MethodAttributes.Public |
				MethodAttributes.HideBySig,
				VoidType);

				method.HasThis = true;

				injectableMonoBehaviour.Type.Methods.Add(method);

				method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
				method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
			}

			method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);

			method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
			method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, methodReference));

			method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
			method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
		}

		public static new InjectionResult Inject(AssemblyDefinition assemblyDefinition)
		{
			return new UnityAssemblyInjector(assemblyDefinition).Result;
		}
	}
}
