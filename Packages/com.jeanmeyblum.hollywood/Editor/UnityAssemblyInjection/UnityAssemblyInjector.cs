using Hollywood.Editor.AssemblyInjection;
using Hollywood.Unity;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	internal class UnityAssemblyInjector : AssemblyInjector
	{
		internal static readonly Type ObservedMonoBehaviourAttributeType = typeof(ObservedMonoBehaviourAttribute);

		private static readonly System.Reflection.MethodInfo NotifyMonoBehaviourCreationMethod = typeof(Unity.Helper.Internal).GetMethod(nameof(Unity.Helper.Internal.NotifyMonoBehaviourCreation), StaticBindingFlags);
		private static readonly System.Reflection.MethodInfo NotifyMonoBehaviourDestructionMethod = typeof(Unity.Helper.Internal).GetMethod(nameof(Unity.Helper.Internal.NotifyMonoBehaviourDestruction), StaticBindingFlags);

		private MethodReference NotifyMonoBehaviourCreationMethodReference;
		private MethodReference NotifyMonoBehaviourDestructionMethodReference;

		protected UnityAssemblyInjector(AssemblyDefinition assemblyDefinition)
			: base(assemblyDefinition)
		{ }

		protected override void Inject()
		{
			NotifyMonoBehaviourCreationMethodReference = AssemblyDefinition.MainModule.ImportReference(NotifyMonoBehaviourCreationMethod);
			NotifyMonoBehaviourDestructionMethodReference = AssemblyDefinition.MainModule.ImportReference(NotifyMonoBehaviourDestructionMethod);

			var injectionData = new UnityInjectionData(AssemblyDefinition.MainModule);

			Inject(injectionData as InjectionData);
			Inject(injectionData);
			MarkAssemblyAsInjected();
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
			InjectMethodCallInMethod(injectableMonoBehaviour, "Awake", NotifyMonoBehaviourCreationMethodReference);
			InjectMethodCallInMethod(injectableMonoBehaviour, "OnDestroy", NotifyMonoBehaviourDestructionMethodReference);
		}

		private void InjectMethodCallInMethod(InjectableMonoBehaviour injectableMonoBehaviour, string methodName, MethodReference methodReference)
		{
			if (!(injectableMonoBehaviour.Type.Methods.FirstOrDefault(m => m.Name == methodName && m.HasThis && m.ReturnType.FullName == VoidType.FullName) is MethodDefinition method))
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
