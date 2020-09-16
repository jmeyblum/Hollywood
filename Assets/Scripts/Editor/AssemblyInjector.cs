using System.Collections.Generic;
using Mono.Cecil;
using System;
using System.Linq;
using Hollywood.Runtime;
using Mono.Cecil.Cil;
using Hollywood.Runtime.Internal;

namespace Hollywood.Editor
{
	// TODO: support IOwner type when base types are IOwner (IOwner implementation must only exists in highest class, this will likely fuck-up if said base class is in another assembly, or maybe not since if base class is in another assembly it has already been compiled.)
	// TODO: support IInjectable type when base types are IInjectable: __ResolveDependencies must be marked as override instead of virtual and not call Hollywood.Runtime.Injector.ResolveOwnedInstances(this); but base.__ResolveDependencies().
	// TODO: add settings to have a list of ignored assemblies


	// TODO: validate that IInjectable and IOwner is not used by user

	internal class AssemblyInjector
	{
		internal static readonly Type OwnsAttributeType = typeof(OwnsAttribute);
		internal static readonly Type OwnsAllAttributeType = typeof(OwnsAllAttribute);
		internal static readonly Type NeedsAttributeType = typeof(NeedsAttribute);

		internal static readonly Type IInjectableType = typeof(IInjectable);
		internal static readonly Type IOwnerType = typeof(IOwner);

		private AssemblyDefinition AssemblyDefinition;

		private InjectionResult Result;

		private AssemblyInjector(AssemblyDefinition assemblyDefinition)
		{
			AssemblyDefinition = assemblyDefinition;

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

			// add IOwner if owner
			bool isOwner = injectableType.ownedInterfaceType.Count > 0;

			if (isOwner)
			{
				InjectIOwner(injectableType);
			}

			InjectIInjectable(injectableType, isOwner);

			// add IDisposable if not here
			// implements constructor
			// implements __ResolveDependencies
			// implements Dispose
		}

		private void InjectIOwner(InjectableType injectableType)
		{
			InterfaceImplementation iownerType = new InterfaceImplementation(AssemblyDefinition.MainModule.ImportReference(IOwnerType));
			injectableType.Type.Interfaces.Add(iownerType);

			var objectHashsetType = AssemblyDefinition.MainModule.ImportReference(typeof(HashSet<object>));
			string backingFieldName = $"<{IOwnerType.FullName}.{nameof(IOwner.__ownedInstances)}>k__BackingField";
			var backingField = new FieldDefinition(backingFieldName, FieldAttributes.Private, objectHashsetType);

			injectableType.Type.Fields.Add(backingField);

			string getOwnedInstancesInterfaceMethodName = $"get_{nameof(IOwner.__ownedInstances)}";
			var ownerInterfaceGetOwnedInstancesMethod = AssemblyDefinition.MainModule.ImportReference(IOwnerType.GetMethod(getOwnedInstancesInterfaceMethodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));

			var getOwnedInstancesMethodName = $"{ownerInterfaceGetOwnedInstancesMethod.DeclaringType}.{ownerInterfaceGetOwnedInstancesMethod.Name}";

			var ownedInstanceGetterMethod = new MethodDefinition(getOwnedInstancesMethodName,
				MethodAttributes.Private |
				MethodAttributes.Final |
				MethodAttributes.HideBySig |
				MethodAttributes.Virtual |
				MethodAttributes.NewSlot |
				MethodAttributes.SpecialName, objectHashsetType);

			ownedInstanceGetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
			ownedInstanceGetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, backingField));
			ownedInstanceGetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			ownedInstanceGetterMethod.Overrides.Add(ownerInterfaceGetOwnedInstancesMethod);

			injectableType.Type.Methods.Add(ownedInstanceGetterMethod);

			string setOwnedInstancesInterfaceMethodName = $"set_{nameof(IOwner.__ownedInstances)}";
			var ownerInterfaceSetOwnedInstancesMethod = AssemblyDefinition.MainModule.ImportReference(IOwnerType.GetMethod(setOwnedInstancesInterfaceMethodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));

			var setOwnedInstancesMethodName = $"{ownerInterfaceSetOwnedInstancesMethod.DeclaringType}.{ownerInterfaceSetOwnedInstancesMethod.Name}";

			var ownedInstancesSetterMethod = new MethodDefinition(setOwnedInstancesMethodName,
				MethodAttributes.Public |
				MethodAttributes.Final |
				MethodAttributes.HideBySig |
				MethodAttributes.Virtual |
				MethodAttributes.NewSlot |
				MethodAttributes.SpecialName, AssemblyDefinition.MainModule.ImportReference(typeof(void)));

			ownedInstancesSetterMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, objectHashsetType));

			ownedInstancesSetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
			ownedInstancesSetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
			ownedInstancesSetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, backingField));
			ownedInstancesSetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
			ownedInstancesSetterMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			ownedInstancesSetterMethod.Overrides.Add(ownerInterfaceSetOwnedInstancesMethod);

			injectableType.Type.Methods.Add(ownedInstancesSetterMethod);

			var ownedInstancesProperty = new PropertyDefinition($"{IOwnerType.FullName}.{nameof(IOwner.__ownedInstances)}", PropertyAttributes.None, objectHashsetType);
			ownedInstancesProperty.GetMethod = ownedInstanceGetterMethod;
			ownedInstancesProperty.SetMethod = ownedInstancesSetterMethod;

			injectableType.Type.Properties.Add(ownedInstancesProperty);

			var constructor = injectableType.Type.Methods.FirstOrDefault(m => m.IsConstructor && !m.HasParameters && !m.IsStatic);
			var existingConstructorWithParameters = injectableType.Type.Methods.FirstOrDefault(m => m.IsConstructor && m.HasParameters && !m.IsStatic);
			bool newConstructor = constructor == null;
			if (newConstructor)
			{
				constructor = new MethodDefinition(".ctor",
					MethodAttributes.Private |
					MethodAttributes.HideBySig |
					MethodAttributes.SpecialName |
					MethodAttributes.RTSpecialName,
					AssemblyDefinition.MainModule.ImportReference(typeof(void)));

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
						else if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == ".ctor")
						{
							break;
						}

						++instructionIndex;
					}

					instructionIndex = 0;
					foreach (var instruction in existingConstructorWithParameters.Body.Instructions)
					{
						constructor.Body.Instructions.Add(instruction);

						if(instructionIndex == lastStfldIndex)
						{
							break;
						}

						++instructionIndex;
					}
				}

				var baseType = AssemblyDefinition.MainModule.ImportReference(injectableType.Type.BaseType);
				var baseConstructor = new MethodReference(".ctor", AssemblyDefinition.MainModule.ImportReference(typeof(void)), baseType);
				baseConstructor.HasThis = true;

				baseConstructor = AssemblyDefinition.MainModule.ImportReference(baseConstructor);

				constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, baseConstructor));
				constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));		
			}

			int instructionInsertionIndex = 0;
			foreach (var instruction in constructor.Body.Instructions)
			{
				++instructionInsertionIndex;

				if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == ".ctor")
				{
					break;
				}
			}

			var createOwnedInstancesInstructions = new List<Instruction>();

			var createOwnedInstanceGenericMethod = typeof(Injector).GetMethod(nameof(Injector.CreateOwnedInstance), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

			var createOwnedInstanceGenericMethodReference = AssemblyDefinition.MainModule.ImportReference(createOwnedInstanceGenericMethod);

			foreach (var ownedType in injectableType.ownedInterfaceType)
			{
				var createOwnedInstanceMethodReference = new GenericInstanceMethod(createOwnedInstanceGenericMethodReference);
				createOwnedInstanceMethodReference.GenericArguments.Add(ownedType);

				createOwnedInstancesInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
				createOwnedInstancesInstructions.Add(Instruction.Create(OpCodes.Ldnull));
				createOwnedInstancesInstructions.Add(Instruction.Create(OpCodes.Call, createOwnedInstanceMethodReference));
				createOwnedInstancesInstructions.Add(Instruction.Create(OpCodes.Nop));
			}

			for(int instructionIndex = createOwnedInstancesInstructions.Count - 1; instructionIndex >= 0; --instructionIndex)
			{
				constructor.Body.Instructions.Insert(instructionInsertionIndex, createOwnedInstancesInstructions[instructionIndex]);
			}
		}

		private void InjectIInjectable(InjectableType injectableType, bool isOwner)
		{
			InterfaceImplementation iinjectableType = new InterfaceImplementation(AssemblyDefinition.MainModule.ImportReference(IInjectableType));
			injectableType.Type.Interfaces.Add(iinjectableType);

			var resolveDependenciesInterfaceMethod = AssemblyDefinition.MainModule.ImportReference(typeof(IInjectable).GetMethod(nameof(IInjectable.__ResolveDependencies), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
			MethodDefinition resolveDependenciesMethod = new MethodDefinition($"{resolveDependenciesInterfaceMethod.DeclaringType}.{resolveDependenciesInterfaceMethod.Name}",
				MethodAttributes.Private |
				MethodAttributes.Final |
				MethodAttributes.HideBySig |
				MethodAttributes.Virtual |
				MethodAttributes.NewSlot,
				AssemblyDefinition.MainModule.ImportReference(typeof(void)));

			resolveDependenciesMethod.Overrides.Add(resolveDependenciesInterfaceMethod);

			resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

			foreach (var neededField in injectableType.neededInterfaceType)
			{
				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

				var resolveDependencyMethod = typeof(Injector).GetMethod(nameof(Injector.ResolveDependency), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

				var resolveDependencyGenericMethodReference = AssemblyDefinition.MainModule.ImportReference(resolveDependencyMethod);

				var resolveDependencyMethodReference = new GenericInstanceMethod(resolveDependencyGenericMethodReference);
				resolveDependencyMethodReference.GenericArguments.Add(neededField.Value);

				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, resolveDependencyMethodReference));
				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, neededField.Key));
			}

			if (isOwner)
			{
				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

				var resolveOwnedInstancesMethod = typeof(Injector).GetMethod(nameof(Injector.ResolveOwnedInstances), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

				var resolveOwnedInstancesMethodReference = AssemblyDefinition.MainModule.ImportReference(resolveOwnedInstancesMethod);

				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, resolveOwnedInstancesMethodReference));
				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
			}

			resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			injectableType.Type.Methods.Add(resolveDependenciesMethod);
		}

		private void Inject(IEnumerable<InjectedInterface> injectedInterfaces)
		{
			if(injectedInterfaces.Count() == 0)
			{
				return;
			}

			Result = InjectionResult.Modified;

			var injectedInterfacesType = new TypeDefinition($"__Hollywood.{AssemblyDefinition.MainModule.Name}", "__InjectedInterfaces", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
			injectedInterfacesType.BaseType = AssemblyDefinition.MainModule.ImportReference(typeof(System.Object));
			var injectedInterfacesConstructor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, AssemblyDefinition.MainModule.ImportReference(typeof(void)));
			var interfaceNamesMember = new FieldDefinition("__interfaceNames", FieldAttributes.Public | FieldAttributes.Static, AssemblyDefinition.MainModule.ImportReference(typeof(string[])));
			injectedInterfacesType.Methods.Add(injectedInterfacesConstructor);
			injectedInterfacesType.Fields.Add(interfaceNamesMember);

			AssemblyDefinition.MainModule.Types.Add(injectedInterfacesType);

			injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, injectedInterfaces.Count()));
			injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, AssemblyDefinition.MainModule.ImportReference(typeof(string))));
			int injectedInterfaceIndex = 0;
			foreach(var injectedInterface in injectedInterfaces)
			{
				injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));

				if (injectedInterfaceIndex <= 8)
				{
					OpCode code = default;
					switch (injectedInterfaceIndex)
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
					injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(code));
				}
				else if(injectedInterfaceIndex < 128)
				{
					injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)injectedInterfaceIndex));
				}
				else
				{
					injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, injectedInterfaceIndex));
				}

				injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, injectedInterface.ToString()));
				injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));

				++injectedInterfaceIndex;
			}

			injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, interfaceNamesMember));
			injectedInterfacesConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
		}

		internal static InjectionResult Inject(AssemblyDefinition assemblyDefinition)
		{
			return new AssemblyInjector(assemblyDefinition).Result;
		}
	}
}