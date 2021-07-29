using System.Collections.Generic;
using Mono.Cecil;
using System;
using System.Linq;
using Hollywood.Runtime;
using Mono.Cecil.Cil;
using Hollywood.Runtime.Internal;
using Mono.Collections.Generic;

namespace Hollywood.Editor.AssemblyInjection
{
	// TODO: validate that __Hollywood_Injected and IOwner is not used by user

	public class AssemblyInjector
	{
		internal static readonly Type InjectorType = typeof(Injector);
		internal static readonly Type OwnsAttributeType = typeof(OwnsAttribute);
		internal static readonly Type OwnsAllAttributeType = typeof(OwnsAllAttribute);
		internal static readonly Type NeedsAttributeType = typeof(NeedsAttribute);
		internal static readonly Type InheritsFromInjectableAttributeType = typeof(InheritsFromInjectableAttribute);

		private static readonly Type HollywoodInjectedType = typeof(__Hollywood_Injected);

		private const System.Reflection.BindingFlags StaticBindingFlags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
		private const System.Reflection.BindingFlags InstanceBindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

		private static readonly System.Reflection.MethodInfo InjectorAddInstanceMethod = typeof(Injector.Advanced).GetMethod(nameof(Injector.Advanced.AddInstance), StaticBindingFlags);
		private static readonly System.Reflection.MethodInfo InjectorAddInstancesMethod = typeof(Injector.Advanced).GetMethod(nameof(Injector.Advanced.AddInstances), StaticBindingFlags);

		private static readonly System.Reflection.MethodInfo FindDependencyMethod = typeof(Injector).GetMethod(nameof(Injector.FindDependency), StaticBindingFlags);

		private static readonly System.Reflection.MethodInfo ResolveOwnedInstancesMethod = typeof(Injector.Internal).GetMethod(nameof(Injector.Internal.ResolveOwnedInstances), StaticBindingFlags);
		private static readonly System.Reflection.MethodInfo DisposeOwnedInstancesMethod = typeof(Injector.Internal).GetMethod(nameof(Injector.Internal.DisposeOwnedInstances), StaticBindingFlags);

		private readonly string ResolveProtectedMethodName;

		private readonly AssemblyDefinition AssemblyDefinition;
		private readonly MethodReference InjectorAddInstanceMethodReference;
		private readonly MethodReference InjectorAddInstancesMethodReference;

		private readonly InterfaceImplementation HollywoodInjectedTypeImplementation;
		private readonly MethodReference ResolveInterfaceMethod;

		private readonly MethodReference FindDependencyGenericMethodReference;

		private readonly TypeReference VoidType;
		private readonly TypeReference ObjectType;
		private readonly TypeReference TypeType;
		private readonly TypeReference TypeArrayType;

		private readonly MethodReference GetTypeFromHandleMethod;

		private InjectionResult Result;

		private AssemblyInjector(AssemblyDefinition assemblyDefinition)
		{
			AssemblyDefinition = assemblyDefinition;

			// For Owners
			InjectorAddInstanceMethodReference = AssemblyDefinition.MainModule.ImportReference(InjectorAddInstanceMethod);
			InjectorAddInstancesMethodReference = AssemblyDefinition.MainModule.ImportReference(InjectorAddInstancesMethod);

			// For Injected
			HollywoodInjectedTypeImplementation = new InterfaceImplementation(AssemblyDefinition.MainModule.ImportReference(HollywoodInjectedType));
			ResolveInterfaceMethod = AssemblyDefinition.MainModule.ImportReference(typeof(__Hollywood_Injected).GetMethod(nameof(__Hollywood_Injected.__Resolve), InstanceBindingFlags));
			FindDependencyGenericMethodReference = AssemblyDefinition.MainModule.ImportReference(FindDependencyMethod);

			VoidType = AssemblyDefinition.MainModule.ImportReference(typeof(void));
			ObjectType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Object));
			TypeType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Type));
			TypeArrayType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Type[]));

			ResolveProtectedMethodName = $"<>{HollywoodInjectedType.Name}<>{ResolveInterfaceMethod.Name}<>";

			GetTypeFromHandleMethod = AssemblyDefinition.MainModule.ImportReference(typeof(System.Type).GetMethod(nameof(Type.GetTypeFromHandle), StaticBindingFlags));

			Inject();
		}

		private void Inject()
		{
			var injectionData = new InjectionData(AssemblyDefinition.MainModule);

			Inject(injectionData);
		}

		private void Inject(InjectionData injectionData)
		{
			Inject(injectionData.InjectableTypes);

			Inject(injectionData.InjectedInterfaces);
		}

		private void Inject(IEnumerable<InjectableType> injectableTypes)
		{
			if (injectableTypes.Count() == 0)
			{
				return;
			}

			Result = InjectionResult.Modified;

			foreach (var injectableType in injectableTypes)
			{
				Inject(injectableType);
			}
		}

		private void Inject(InjectableType injectableType)
		{
			Result = InjectionResult.Modified;

			bool isOwner = injectableType.ownedInterfaceType.Count > 0 || injectableType.ownedAllInterfaceType.Count > 0;

			if (isOwner)
			{
				InjectIOwner(injectableType);
			}

			InjectHollywoodInjected(injectableType, isOwner);
		}

		private void InjectIOwner(InjectableType injectableType)
		{
			var constructor = GetDefaultConstructor(injectableType);
			var instructionInsertionIndex = FindValidInstructionInsertionIndex(constructor);

			var addOwnedInstancesInstructions = new List<Instruction>();

			AddInstructionsForOwner(InjectorAddInstanceMethodReference, injectableType.ownedInterfaceType, addOwnedInstancesInstructions);
			AddInstructionsForOwner(InjectorAddInstancesMethodReference, injectableType.ownedAllInterfaceType, addOwnedInstancesInstructions);

			for (int instructionIndex = addOwnedInstancesInstructions.Count - 1; instructionIndex >= 0; --instructionIndex)
			{
				constructor.Body.Instructions.Insert(instructionInsertionIndex, addOwnedInstancesInstructions[instructionIndex]);
			}
		}

		private void AddInstructionsForOwner(MethodReference addInstanceGenericMethod, IEnumerable<TypeReference> ownedTypes, List<Instruction> instructions)
		{
			foreach (var ownedType in ownedTypes)
			{
				var addInstanceMethodReference = new GenericInstanceMethod(addInstanceGenericMethod);
				addInstanceMethodReference.GenericArguments.Add(ownedType);

				instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				instructions.Add(Instruction.Create(OpCodes.Call, addInstanceMethodReference));
				instructions.Add(Instruction.Create(OpCodes.Pop));
				instructions.Add(Instruction.Create(OpCodes.Nop));
			}
		}

		private static int FindValidInstructionInsertionIndex(MethodDefinition constructor)
		{
			int instructionInsertionIndex = 0;
			foreach (var instruction in constructor.Body.Instructions)
			{
				++instructionInsertionIndex;

				if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == Constants.InstanceInitializerMethodName)
				{
					break;
				}
			}

			return instructionInsertionIndex;
		}

		private MethodDefinition GetDefaultConstructor(InjectableType injectableType)
		{
			MethodDefinition constructor = injectableType.Type.Methods.FirstOrDefault(m => m.IsConstructor && !m.HasParameters && !m.IsStatic);
			var existingConstructorWithParameters = injectableType.Type.Methods.FirstOrDefault(m => m.IsConstructor && m.HasParameters && !m.IsStatic);
			bool newConstructor = constructor == null;
			if (newConstructor)
			{
				constructor = new MethodDefinition(Constants.InstanceInitializerMethodName,
					MethodAttributes.Private |
					MethodAttributes.HideBySig |
					MethodAttributes.SpecialName |
					MethodAttributes.RTSpecialName,
					VoidType);

				injectableType.Type.Methods.Add(constructor);

				if (existingConstructorWithParameters != null)
				{
					int lastStfldIndex = -1;
					int instructionIndex = 0;
					foreach (var instruction in existingConstructorWithParameters.Body.Instructions)
					{
						if (instruction.OpCode == OpCodes.Stfld)
						{
							lastStfldIndex = instructionIndex;
						}
						else if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == Constants.InstanceInitializerMethodName)
						{
							break;
						}

						++instructionIndex;
					}

					instructionIndex = 0;
					foreach (var instruction in existingConstructorWithParameters.Body.Instructions)
					{
						constructor.Body.Instructions.Add(instruction);

						if (instructionIndex == lastStfldIndex)
						{
							break;
						}

						++instructionIndex;
					}
				}

				var baseType = AssemblyDefinition.MainModule.ImportReference(injectableType.Type.BaseType);
				var baseConstructor = new MethodReference(Constants.InstanceInitializerMethodName, VoidType, baseType);
				baseConstructor.HasThis = true;

				baseConstructor = AssemblyDefinition.MainModule.ImportReference(baseConstructor);

				constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, baseConstructor));
				constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
			}

			return constructor;
		}

		private void InjectHollywoodInjected(InjectableType injectableType, bool isOwner)
		{
			injectableType.Type.Interfaces.Add(HollywoodInjectedTypeImplementation);

			AddResolveMethod(injectableType, isOwner);
		}

		private void AddResolveMethod(InjectableType injectableType, bool isOwner)
		{
			MethodDefinition resolveProtectedMethod = AddResolveProtectedMethod(injectableType);

			AddHollywoodInjectedResolveMethod(injectableType, resolveProtectedMethod);
		}

		private MethodDefinition AddResolveProtectedMethod(InjectableType injectableType)
		{
			MethodDefinition resolveProtectedMethod = new MethodDefinition(
				ResolveProtectedMethodName,
				MethodAttributes.Family |
				MethodAttributes.HideBySig |
				MethodAttributes.Virtual,
				VoidType);

			resolveProtectedMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

			AddFindDependenciesInstructions(injectableType, resolveProtectedMethod);

			if (injectableType.InjectableBaseType != null)
			{
				MethodReference baseResolveProtectedMethod = new MethodReference(
					ResolveProtectedMethodName,
					VoidType,
					injectableType.InjectableBaseType);

				baseResolveProtectedMethod.HasThis = true;

				resolveProtectedMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				resolveProtectedMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, baseResolveProtectedMethod));
				resolveProtectedMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
			}
			else
			{
				resolveProtectedMethod.Attributes |= MethodAttributes.NewSlot;
			}

			resolveProtectedMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			injectableType.Type.Methods.Add(resolveProtectedMethod);
			return resolveProtectedMethod;
		}

		private void AddHollywoodInjectedResolveMethod(InjectableType injectableType, MethodDefinition resolveProtectedMethod)
		{
			MethodDefinition resolveMethod = new MethodDefinition($"{ResolveInterfaceMethod.DeclaringType}.{ResolveInterfaceMethod.Name}",
				MethodAttributes.Private |
				MethodAttributes.Final |
				MethodAttributes.HideBySig |
				MethodAttributes.Virtual |
				MethodAttributes.NewSlot,
				VoidType);

			resolveMethod.Overrides.Add(ResolveInterfaceMethod);

			resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

			resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
			resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, resolveProtectedMethod));
			resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

			resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			injectableType.Type.Methods.Add(resolveMethod);
		}

		private void AddFindDependenciesInstructions(InjectableType injectableType, MethodDefinition resolveMethod)
		{
			foreach (var neededField in injectableType.neededInterfaceType)
			{
				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

				var resolveDependencyMethodReference = new GenericInstanceMethod(FindDependencyGenericMethodReference);
				resolveDependencyMethodReference.GenericArguments.Add(neededField.Value);

				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, resolveDependencyMethodReference));
				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, neededField.Key));
			}
		}

		private void Inject(IEnumerable<InjectedInterface> injectedInterfaces)
		{
			if (injectedInterfaces.Count() == 0)
			{
				return;
			}

			Result = InjectionResult.Modified;

			var injectedInterfacesType = new TypeDefinition(string.Format(Constants.DefaultTypeResolver.AssemblyNameTemplate, AssemblyDefinition.MainModule.Name), Constants.DefaultTypeResolver.TypeName, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
			injectedInterfacesType.BaseType = ObjectType;
			var injectedInterfacesConstructor = new MethodDefinition(Constants.TypeInitializerMethodName, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, VoidType);
			var interfacesMember = new FieldDefinition(Constants.DefaultTypeResolver.MemberName, FieldAttributes.Public | FieldAttributes.Static, TypeArrayType);
			injectedInterfacesType.Methods.Add(injectedInterfacesConstructor);
			injectedInterfacesType.Fields.Add(interfacesMember);

			AssemblyDefinition.MainModule.Types.Add(injectedInterfacesType);

			var instructions = injectedInterfacesConstructor.Body.Instructions;

			AddNewTypeArrayInstructions(injectedInterfaces.Select(s => s.Type), instructions);

			instructions.Add(Instruction.Create(OpCodes.Stsfld, interfacesMember));
			instructions.Add(Instruction.Create(OpCodes.Ret));
		}

		public void AddNewTypeArrayInstructions(IEnumerable<TypeReference> arrayValues, Collection<Instruction> instructions)
		{
			instructions.Add(Instruction.Create(OpCodes.Ldc_I4, arrayValues.Count()));
			instructions.Add(Instruction.Create(OpCodes.Newarr, TypeType));
			int valueIndex = 0;

			foreach (var arrayValue in arrayValues)
			{
				instructions.Add(Instruction.Create(OpCodes.Dup));

				if (valueIndex <= 8)
				{
					OpCode code = default;
					switch (valueIndex)
					{
						case 0:
							code = OpCodes.Ldc_I4_0;
							break;
						case 1:
							code = OpCodes.Ldc_I4_1;
							break;
						case 2:
							code = OpCodes.Ldc_I4_2;
							break;
						case 3:
							code = OpCodes.Ldc_I4_3;
							break;
						case 4:
							code = OpCodes.Ldc_I4_4;
							break;
						case 5:
							code = OpCodes.Ldc_I4_5;
							break;
						case 6:
							code = OpCodes.Ldc_I4_6;
							break;
						case 7:
							code = OpCodes.Ldc_I4_7;
							break;
						case 8:
							code = OpCodes.Ldc_I4_8;
							break;
					}
					instructions.Add(Instruction.Create(code));
				}
				else if (valueIndex < 128)
				{
					instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)valueIndex));
				}
				else
				{
					instructions.Add(Instruction.Create(OpCodes.Ldc_I4, valueIndex));
				}

				instructions.Add(Instruction.Create(OpCodes.Ldtoken, arrayValue));
				instructions.Add(Instruction.Create(OpCodes.Call, GetTypeFromHandleMethod));
				instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));

				++valueIndex;
			}
		}

		public static InjectionResult Inject(AssemblyDefinition assemblyDefinition)
		{
			return new AssemblyInjector(assemblyDefinition).Result;
		}
	}
}