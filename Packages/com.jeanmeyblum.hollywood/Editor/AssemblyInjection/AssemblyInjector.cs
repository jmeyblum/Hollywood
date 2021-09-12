using Hollywood.Controller;
using Hollywood.Internal;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Hollywood.Editor.UnityAssemblyInjection")]

namespace Hollywood.Editor.AssemblyInjection
{
	internal class AssemblyInjector
	{
		internal static readonly Type InjectorType = typeof(Injector);
		internal static readonly Type OwnsAttributeType = typeof(OwnsAttribute);
		internal static readonly Type OwnsAllAttributeType = typeof(OwnsAllAttribute);
		internal static readonly Type NeedsAttributeType = typeof(NeedsAttribute);
		internal static readonly Type InheritsFromInjectableAttributeType = typeof(InheritsFromInjectableAttribute);
		internal static readonly Type IncludeTypeAttributeType = typeof(IncludeTypeAttribute);
		internal static readonly Type IgnoreTypeAttributeType = typeof(IgnoreTypeAttribute);
		internal static readonly Type IItemObserverGenericType = typeof(IItemObserver<>);

		internal static readonly Type IControlledItemType = typeof(IControlledItem);
		internal static readonly Type IItemControllerType = typeof(IItemController);

		internal static readonly Type ItemControllerType = typeof(ItemController<,>);
		internal static readonly Type ItemsControllerType = typeof(ItemsController<,>);

		private static readonly Type HollywoodInjectedType = typeof(__Hollywood_Injected);
		private static readonly Type HollywoodItemObserverType = typeof(__Hollywood_ItemObserver);

		private static readonly Type HollywoodPostProcessed = typeof(__Hollywood_PostProcessedAttribute);

		internal const System.Reflection.BindingFlags StaticBindingFlags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
		internal const System.Reflection.BindingFlags InstanceBindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

		private static readonly System.Reflection.MethodInfo InjectorAddInstanceMethod = typeof(Injector.Advanced).GetMethod(nameof(Injector.Advanced.AddInstance), StaticBindingFlags);
		private static readonly System.Reflection.MethodInfo InjectorAddInstancesMethod = typeof(Injector.Advanced).GetMethod(nameof(Injector.Advanced.AddInstances), StaticBindingFlags);

		private static readonly System.Reflection.MethodInfo FindDependencyMethod = typeof(Injector).GetMethod(nameof(Injector.FindDependency), StaticBindingFlags);

		private static readonly System.Reflection.MethodInfo InjectorRegisterItemObserverMethod = typeof(Injector.Internal).GetMethod(nameof(Injector.Internal.RegisterItemObserver), StaticBindingFlags);
		private static readonly System.Reflection.MethodInfo InjectorUnregisterItemObserverMethod = typeof(Injector.Internal).GetMethod(nameof(Injector.Internal.UnregisterItemObserver), StaticBindingFlags);

		private readonly string ResolveProtectedMethodName;

		private readonly MethodReference InjectorAddInstanceMethodReference;
		private readonly MethodReference InjectorAddInstancesMethodReference;

		private readonly InterfaceImplementation HollywoodInjectedTypeImplementation;
		private readonly MethodReference ResolveInterfaceMethod;

		private readonly MethodReference FindDependencyGenericMethodReference;

		private readonly InterfaceImplementation HollywoodItemObserverTypeImplementation;
		private readonly MethodReference RegisterItemObserverMethod;
		private readonly MethodReference UnregisterItemObserverMethod;

		private readonly MethodReference InjectorRegisterItemObserverMethodReference;
		private readonly MethodReference InjectorUnregisterItemObserverMethodReference;

		private readonly MethodReference HollywoodPostProcessedConstructorReference;

		internal readonly TypeReference VoidType;
		internal readonly TypeReference ObjectType;
		internal readonly TypeReference TypeType;
		internal readonly TypeReference TypeArrayType;
		internal readonly TypeReference TypeArrayArrayType;

		private readonly MethodReference GetTypeFromHandleMethod;

		protected readonly AssemblyDefinition AssemblyDefinition;
		protected InjectionResult Result;

		protected AssemblyInjector(AssemblyDefinition assemblyDefinition)
		{
			AssemblyDefinition = assemblyDefinition;

			if (AssemblyDefinition.CustomAttributes.Any(c => c.AttributeType.FullName == HollywoodPostProcessed.FullName))
			{
				Result = InjectionResult.Failed;

				throw new AssemblyAlreadyInjectedException($"Assembly {assemblyDefinition.Name} was already injected.");
			}

			// For Owners
			InjectorAddInstanceMethodReference = AssemblyDefinition.MainModule.ImportReference(InjectorAddInstanceMethod);
			InjectorAddInstancesMethodReference = AssemblyDefinition.MainModule.ImportReference(InjectorAddInstancesMethod);

			// For Injected
			HollywoodInjectedTypeImplementation = new InterfaceImplementation(AssemblyDefinition.MainModule.ImportReference(HollywoodInjectedType));
			ResolveInterfaceMethod = AssemblyDefinition.MainModule.ImportReference(HollywoodInjectedType.GetMethod(nameof(__Hollywood_Injected.__Resolve), InstanceBindingFlags));
			FindDependencyGenericMethodReference = AssemblyDefinition.MainModule.ImportReference(FindDependencyMethod);

			// For ItemObserver
			HollywoodItemObserverTypeImplementation = new InterfaceImplementation(AssemblyDefinition.MainModule.ImportReference(HollywoodItemObserverType));
			RegisterItemObserverMethod = AssemblyDefinition.MainModule.ImportReference(HollywoodItemObserverType.GetMethod(nameof(__Hollywood_ItemObserver.__Register), InstanceBindingFlags));
			UnregisterItemObserverMethod = AssemblyDefinition.MainModule.ImportReference(HollywoodItemObserverType.GetMethod(nameof(__Hollywood_ItemObserver.__Unregister), InstanceBindingFlags));

			InjectorRegisterItemObserverMethodReference = AssemblyDefinition.MainModule.ImportReference(InjectorRegisterItemObserverMethod);
			InjectorUnregisterItemObserverMethodReference = AssemblyDefinition.MainModule.ImportReference(InjectorUnregisterItemObserverMethod);

			HollywoodPostProcessedConstructorReference = AssemblyDefinition.MainModule.ImportReference(HollywoodPostProcessed.GetConstructor(Type.EmptyTypes));

			VoidType = AssemblyDefinition.MainModule.ImportReference(typeof(void));
			ObjectType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Object));
			TypeType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Type));
			TypeArrayType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Type[]));
			TypeArrayArrayType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Type[][]));

			ResolveProtectedMethodName = $"<>{HollywoodInjectedType.Name}<>{ResolveInterfaceMethod.Name}<>";

			GetTypeFromHandleMethod = AssemblyDefinition.MainModule.ImportReference(typeof(System.Type).GetMethod(nameof(Type.GetTypeFromHandle), StaticBindingFlags));

			Inject();
		}

		protected virtual void Inject()
		{
			var injectionData = new InjectionData(AssemblyDefinition.MainModule);

			Inject(injectionData);
			MarkAssemblyAsInjected();
		}

		protected void MarkAssemblyAsInjected()
		{
			if (Result == InjectionResult.Modified && !AssemblyDefinition.CustomAttributes.Any(c => c.AttributeType.FullName == HollywoodPostProcessed.FullName))
			{
				var attribute = new CustomAttribute(HollywoodPostProcessedConstructorReference);
				AssemblyDefinition.CustomAttributes.Add(attribute);
			}
		}

		protected void Inject(InjectionData injectionData)
		{
			Inject(injectionData.InjectableTypes);

			Inject(injectionData.InjectedTypes.Where(t => !t.IsGenericParameter));
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

			bool isOwner = injectableType.OwnedTypes.Count > 0 || injectableType.OwnedAllTypes.Count > 0;

			if (isOwner)
			{
				InjectIOwner(injectableType);
			}

			bool hasNeeds = injectableType.NeededTypes.Count > 0;

			if (hasNeeds || isOwner || injectableType.InjectableBaseType != null)
			{
				InjectHollywoodInjected(injectableType, isOwner);
			}

			bool isItemObserver = injectableType.ObservedTypes.Count > 0;

			if (isItemObserver)
			{
				InjectItemObserver(injectableType);
			}
		}

		private void InjectIOwner(InjectableType injectableType)
		{
			var constructor = GetDefaultConstructor(injectableType);
			var instructionInsertionIndex = FindValidInstructionInsertionIndex(constructor);

			var addOwnedInstancesInstructions = new List<Instruction>();

			AddInstructionsForOwner(InjectorAddInstanceMethodReference, injectableType.OwnedTypes, addOwnedInstancesInstructions);
			AddInstructionsForOwner(InjectorAddInstancesMethodReference, injectableType.OwnedAllTypes, addOwnedInstancesInstructions);

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
			TypeDefinition declaringType = injectableType.Type;

			GenericInstanceType instanceType = null;
			if (declaringType.HasGenericParameters)
			{
				instanceType = new GenericInstanceType(declaringType);
				foreach (var parameter in declaringType.GenericParameters)
				{
					instanceType.GenericArguments.Add(parameter);
				}
			}

			foreach (var neededField in injectableType.NeededTypes)
			{
				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

				var resolveDependencyMethodReference = new GenericInstanceMethod(FindDependencyGenericMethodReference);
				resolveDependencyMethodReference.GenericArguments.Add(neededField.Value.Item1);

				resolveMethod.Body.Instructions.Add(Instruction.Create(neededField.Value.Item2 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, resolveDependencyMethodReference));

				FieldReference field = neededField.Key;
				if (declaringType.HasGenericParameters)
				{
					field = new FieldReference(neededField.Key.Name, neededField.Key.FieldType, instanceType);
				}

				resolveMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
			}
		}

		private void InjectItemObserver(InjectableType injectableType)
		{
			injectableType.Type.Interfaces.Add(HollywoodItemObserverTypeImplementation);

			AddHollywoodItemObserverMethod(injectableType, RegisterItemObserverMethod, InjectorRegisterItemObserverMethodReference);
			AddHollywoodItemObserverMethod(injectableType, UnregisterItemObserverMethod, InjectorUnregisterItemObserverMethodReference);
		}

		private void AddHollywoodItemObserverMethod(InjectableType injectableType, MethodReference interfaceMethod, MethodReference injectorMethod)
		{
			MethodDefinition interfaceMethodDefinition = new MethodDefinition($"{interfaceMethod.DeclaringType}.{interfaceMethod.Name}",
				MethodAttributes.Private |
				MethodAttributes.Final |
				MethodAttributes.HideBySig |
				MethodAttributes.Virtual |
				MethodAttributes.NewSlot,
				VoidType);

			interfaceMethodDefinition.Overrides.Add(interfaceMethod);

			interfaceMethodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

			foreach (var observedType in injectableType.ObservedTypes)
			{
				var injectorGenericMethod = new GenericInstanceMethod(injectorMethod);
				injectorGenericMethod.GenericArguments.Add(observedType);

				interfaceMethodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				interfaceMethodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Call, injectorGenericMethod));
				interfaceMethodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
			}

			interfaceMethodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			injectableType.Type.Methods.Add(interfaceMethodDefinition);
		}

		private void Inject(IEnumerable<TypeReference> injectedTypes)
		{
			if (injectedTypes.Count() == 0)
			{
				return;
			}

			Result = InjectionResult.Modified;

			var injectedTypesType = new TypeDefinition(string.Format(Constants.DefaultTypeResolver.AssemblyNameTemplate, AssemblyDefinition.MainModule.Name), Constants.DefaultTypeResolver.TypeName, TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
			injectedTypesType.BaseType = ObjectType;
			var injectedTypesConstructor = new MethodDefinition(Constants.TypeInitializerMethodName, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, VoidType);
			var typesMember = new FieldDefinition(Constants.DefaultTypeResolver.MemberName, FieldAttributes.Public | FieldAttributes.Static, TypeArrayArrayType);
			injectedTypesType.Methods.Add(injectedTypesConstructor);
			injectedTypesType.Fields.Add(typesMember);

			AssemblyDefinition.MainModule.Types.Add(injectedTypesType);

			var instructions = injectedTypesConstructor.Body.Instructions;

			AddNewTypeArrayInstructions(injectedTypes, instructions);

			instructions.Add(Instruction.Create(OpCodes.Stsfld, typesMember));
			instructions.Add(Instruction.Create(OpCodes.Ret));
		}

		public void AddNewTypeArrayInstructions(IEnumerable<TypeReference> arrayValues, Collection<Instruction> instructions)
		{
			instructions.Add(CreatePushIntToStackInstruction(arrayValues.Count()));
			instructions.Add(Instruction.Create(OpCodes.Newarr, TypeArrayType));
			int valueIndex = 0;

			foreach (var arrayValue in arrayValues)
			{
				instructions.Add(Instruction.Create(OpCodes.Dup));
				instructions.Add(CreatePushIntToStackInstruction(valueIndex));

				if (arrayValue is TypeDefinition typeDefinition && typeDefinition.HasInterfaces)
				{
					var interfaces = typeDefinition.Interfaces;

					instructions.Add(CreatePushIntToStackInstruction(interfaces.Count + 1));
					instructions.Add(Instruction.Create(OpCodes.Newarr, TypeType));

					AddTypeToArray(instructions, 0, arrayValue);

					int subValueIndex = 1;

					foreach (var @interface in interfaces)
					{
						AddTypeToArray(instructions, subValueIndex, @interface.InterfaceType);

						++subValueIndex;
					}
				}
				else
				{
					instructions.Add(CreatePushIntToStackInstruction(1));
					instructions.Add(Instruction.Create(OpCodes.Newarr, TypeType));

					AddTypeToArray(instructions, 0, arrayValue);
				}

				instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));

				++valueIndex;
			}
		}

		private void AddTypeToArray(Collection<Instruction> instructions, int subValueIndex, TypeReference interfaceTypeReference)
		{
			instructions.Add(Instruction.Create(OpCodes.Dup));
			instructions.Add(CreatePushIntToStackInstruction(subValueIndex));

			instructions.Add(Instruction.Create(OpCodes.Ldtoken, interfaceTypeReference));
			instructions.Add(Instruction.Create(OpCodes.Call, GetTypeFromHandleMethod));

			instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
		}

		private static Instruction CreatePushIntToStackInstruction(int valueIndex)
		{
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
				return Instruction.Create(code);
			}
			else if (valueIndex < 128)
			{
				return Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)valueIndex);
			}
			else
			{
				return Instruction.Create(OpCodes.Ldc_I4, valueIndex);
			}
		}

		public static InjectionResult Inject(AssemblyDefinition assemblyDefinition)
		{
			return new AssemblyInjector(assemblyDefinition).Result;
		}
	}
}