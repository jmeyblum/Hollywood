using System.Collections.Generic;
using Mono.Cecil;
using UnityEditor.Compilation;
using System;
using System.Linq;
using Hollywood.Runtime;
using Mono.Cecil.Cil;
using Hollywood.Runtime.Internal;

namespace Hollywood.Editor
{
	// TODO: List type that will be IInjectable;
	// TODO: List type that will be owners
	// TODO: List all injected interfaces
	// TODO: Generate __InjectedInterfaces.
	// TODO: For all owners modify constructor to add Hollywood.Runtime.Injector.CreateOwnedInstance<TOwnedType>(this);, add IOwner interface, implement it;
	// TODO: For all injectables add IInjectable, implement virtual __ResolveDependencies like so:
	//  - for each needs field : _field = Hollywood.Runtime.Injector.ResolveDependency<IFieldType>();
	//  - after all fields if it is an owner : Hollywood.Runtime.Injector.ResolveOwnedInstances(this);
	// TODO: support IOwner type when base types are IOwner (IOwner implementation must only exists in highest class, this will likely fuck-up if said base class is in another assembly, or maybe not since if base class is in another assembly it has already been compiled.)
	// TODO: support IInjectable type when base types are IInjectable: __ResolveDependencies must be marked as override instead of virtual and not call Hollywood.Runtime.Injector.ResolveOwnedInstances(this); but base.__ResolveDependencies().
	// add settings to have a list of ignored assemblies


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

			string setOwnedInstancesMethodName = $"set_{nameof(IOwner.__ownedInstances)}";
			var ownerInterfaceSetOwnedInstancesMethod = AssemblyDefinition.MainModule.ImportReference(IOwnerType.GetMethod(setOwnedInstancesMethodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));

			var SetOwnedInstancesMethodName = $"{ownerInterfaceSetOwnedInstancesMethod.DeclaringType}.{ownerInterfaceSetOwnedInstancesMethod.Name}";

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

			// add IInjectable
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

			foreach(var neededField in injectableType.neededInterfaceType)
			{
				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

				var resolveDependencyMethod = typeof(Injector).GetMethod(nameof(Injector.ResolveDependency), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);			

				var resolveDependencyGenericMethodReference = AssemblyDefinition.MainModule.ImportReference(resolveDependencyMethod);

				var resolveDependencyMethodReference = new GenericInstanceMethod(resolveDependencyGenericMethodReference);
				resolveDependencyMethodReference.GenericArguments.Add(neededField.Value);

				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, resolveDependencyMethodReference));
				resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, neededField.Key));
			}

			resolveDependenciesMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			injectableType.Type.Methods.Add(resolveDependenciesMethod);

			// add IDisposable if not here
			// implements constructor
			// implements __ResolveDependencies
			// implements Dispose
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

/*

// collect all compose interfaces and all inject interfaces
// map all interfaces to concrete types
// tranform:

[Compose(ISomeComposable)]
[ComposeAll(ISomeComposableAll)]
public class Something
{
    [Inject]
    IInjectedStuff _iInjectedStuff;
}

// to:
[Owns(ISomeComposable)]
[Owns(ISomeComposableAll)]
public class Something : IDisposable, IOwner
{
    [Needs]
    IInjectedStuff _iInjectedStuff;

    public HashSet<object> __composedItems;

    private bool __disposedValue;

    public Something()
    {
        _iInjectedStuff = Injector.Get<IInjectedStuff>();

        Injector.PushCurrentOwner(this);

        Injector.Add<ISomeComposable>(__composedItems);
        Injector.Add<ISomeComposableAll>(__composedItems);

        Injector.PopCurrentOwner(this);
    }

    void IDisposable.Dispose()
    {
        if (!__disposedValue)
        {
            __someComposable.Dispose();
            _someComposableAlls.Dispose();

            __disposedValue = true;
        }
    }
}

public static class Injector
{
    public static Dictionary<Type, Action<object>> _sCreator;
    public static Context CurrentContext;
    public static Stack<object> CurrentOwnerStack;
    public static T Get<T>()
    {
        foreach (var owner in CurrentOwnerStack)
        {
            if (typeof(T).IsAssignableFrom(owner.GetType()))
            {
                return (T)owner;
            }
            if (owner is IOwner iowner)
            {
                foreach (var owned in iowner.owneds)
                {
                    if (typeof(T).IsAssignableFrom(owned.GetType()))
                    {
                        return (T)owned;
                    }
                }
            }
        }

        Debug.LogAssertion($"No {typeof(T)} found.");
    }

    public static void Add<T>(HashSet<object> collection, Context context = default)
    {
        foreach (var obj in collection)
        {
            if (typeof(T).IsAssignableFrom(obj.GetType()))
            {
                return;
            }
        }

        context = context ?? CurrentContext;

        instanceType = context.GetUnique<T>();

        Assert.IsNotNull(instanceType);

        return _sCreator[instanceType]();
    }
}

*/
