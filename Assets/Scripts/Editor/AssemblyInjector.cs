using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Cecil;
using UnityEditor;
using UnityEditor.Compilation;
using System;
using System.Linq;
using UnityEngine.Assertions;
using Hollywood.Runtime;
using Mono.Cecil.Cil;

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

    public static class AssemblyInjector
    {
        private static readonly Type OwnsAttributeType = typeof(OwnsAttribute);
        private static readonly Type OwnsAllAttributeType = typeof(OwnsAllAttribute);
        private static readonly Type NeedsAttributeType = typeof(NeedsAttribute);

        [InitializeOnLoadMethod]
        public static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        private class InjectionType
        {
            public HashSet<string> ownedInterfaceType = new HashSet<string>();
            public HashSet<string> ownedAllInterfaceType = new HashSet<string>();
            public HashSet<string> neededInterfaceType = new HashSet<string>();
        }

        private class InjectionInterface
        {

        }

        private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            Debug.Log(assemblyPath);

            if (compilerMessages.Any(msg => msg.type == CompilerMessageType.Error) == true)
            {
                return;
            }

            if (assemblyPath.Contains("Unity"))
            {
                return;
            }

            if (!assemblyPath.Contains("Assembly.C"))
            {
                return;
            }

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters(ReadingMode.Immediate) { ReadSymbols = true, ReadWrite = true, AssemblyResolver = new DefaultAssemblyResolver() });

            var moduleDefinition = assemblyDefinition.MainModule;

            var injectedTypes = new Dictionary<TypeDefinition, InjectionType>();
            var interfaces = new HashSet<string>();

            foreach (var typeDefinition in moduleDefinition.Types)
            {
                InjectionType injectedType = null;

                var ownsAttributes = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == OwnsAttributeType.FullName);
                if (ownsAttributes.Any())
                {
                    injectedType = injectedType ?? new InjectionType();

                    foreach (var ownsAttribute in ownsAttributes)
                    {
                        var ownedType = ownsAttribute.ConstructorArguments.First().Value as TypeReference;

                        string name = ownedType.FullName;
                        injectedType.ownedInterfaceType.Add(name);
                        interfaces.Add(name);
                    }
                }

                var ownsAllAttributes = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == OwnsAllAttributeType.FullName);
                if (ownsAllAttributes.Any())
                {
                    injectedType = injectedType ?? new InjectionType();

                    foreach (var ownsAllAttribute in ownsAllAttributes)
                    {
                        var ownedAllType = ownsAllAttribute.ConstructorArguments.First().Value as TypeReference;

                        string name = ownedAllType.FullName;
                        injectedType.ownedAllInterfaceType.Add(name);
                        interfaces.Add(name);
                    }
                }

                var neededFields = typeDefinition.Fields.Where(f => f.CustomAttributes.Any(a => a.AttributeType.FullName == NeedsAttributeType.FullName));
                if (neededFields.Any())
                {
                    injectedType = injectedType ?? new InjectionType();

                    foreach (var neededField in neededFields)
                    {
                        var interfaceType = neededField.FieldType;

                        interfaces.Add(interfaceType.FullName);
                    }
                }

                if (injectedType != null)
                {
                    injectedTypes[typeDefinition] = injectedType;
                }
            }


            var warmupTypeDef = new TypeDefinition($"__Hollywood.{assemblyDefinition.MainModule.Name}", "__WarmUpInjectedInterfaces", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
            warmupTypeDef.BaseType = moduleDefinition.ImportReference(typeof(System.Object));

            var interfacesMember = new FieldDefinition("__interfaces", FieldAttributes.Public | FieldAttributes.Static, moduleDefinition.ImportReference(typeof(string[])));

            var warmupConstructor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, moduleDefinition.ImportReference(typeof(void)));

            CreateStringArray(moduleDefinition, interfacesMember, warmupConstructor);

            warmupTypeDef.Methods.Add(warmupConstructor);
            warmupTypeDef.Fields.Add(interfacesMember);

            moduleDefinition.Types.Add(warmupTypeDef);

            /*
            //             var moduleType = moduleDefinition.Types.FirstOrDefault(t => t.Name == "<Module>");
            // 
            //             if(moduleType != null)
            //             {
            //                 var method = moduleType.Methods.FirstOrDefault(m => m.IsStatic && m.Name == ".cctor");
            // 
            //                 if (method == null)
            //                 {
            //                     var attributes = MethodAttributes.Private
            //                                      | MethodAttributes.HideBySig
            //                                      | MethodAttributes.Static
            //                                      | MethodAttributes.SpecialName
            //                                      | MethodAttributes.RTSpecialName;
            //                     method = new MethodDefinition(".cctor", attributes, moduleDefinition.ImportReference(typeof(void)));
            //                     moduleType.Methods.Add(method);
            //                     method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            //                 }
            // 
            //                 var body = method.Body;
            // 
            //                 var logref = moduleDefinition.ImportReference(typeof(TestLog).GetMethod("Log", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public));
            // 
            //                 if (logref != null)
            //                 {
            //                     List<Instruction> instructions = new List<Instruction>();
            //                     instructions.Add(Instruction.Create(OpCodes.Call, logref));
            //                     instructions.Add(Instruction.Create(OpCodes.Ret));
            // 
            //                     body.Instructions.Clear();
            //                     foreach (var ins in instructions)
            //                     {
            //                         body.Instructions.Add(ins);
            //                     }
            //                 }                
            //             }
            */

            assemblyDefinition.Write(new WriterParameters { WriteSymbols = true });

            //
            // loop through types to fetch

            assemblyDefinition.Dispose();
        }

        private static void CreateStringArray(ModuleDefinition moduleDefinition, FieldDefinition interfacesMember, MethodDefinition warmupConstructor)
        {
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 12));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, moduleDefinition.ImportReference(typeof(string))));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "BBBBBB"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_2));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "CCCCC"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_3));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "DDDDD"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_4));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "EEEEEE"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_5));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "FFFFFF"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_6));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "GGGGGG"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_7));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "HHHHHH"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_8));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "IIIIII"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)9));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "JJJJJJ"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)10));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "KKKKKK"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)11));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "LLLLLLL"));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, interfacesMember));
            warmupConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
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