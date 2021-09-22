using Hollywood.Controller;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace Hollywood.Editor.AssemblyInjection
{
	internal class InjectionData
	{
		private const string GetInstanceMethodName = nameof(Hollywood.Injector.GetInstance);
		private const string GetInstancesMethodName = nameof(Hollywood.Injector.GetInstances);
		private const string AddInstanceMethodName = nameof(Hollywood.Injector.Advanced.AddInstance);
		private const string AddInstancesMethodName = nameof(Hollywood.Injector.Advanced.AddInstances);

		private readonly string InjectorAdvancedTypeName = $"{AssemblyInjector.InjectorType.FullName}/{nameof(Hollywood.Injector.Advanced)}";

		public IEnumerable<InjectableType> InjectableTypes { get; private set; }
		public IEnumerable<TypeReference> InjectedTypes { get; private set; }

		public InjectionData(ModuleDefinition moduleDefinition)
		{
			var injectableTypes = new List<InjectableType>();
			var injectedTypes = new HashSet<TypeReference>(new TypeReferenceComprarer());

			foreach (var typeDefinition in moduleDefinition.Types)
			{
				bool hasIgnoreTypeAttribute = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.IgnoreTypeAttributeType.FullName).Any();
				if (hasIgnoreTypeAttribute)
				{
					continue;
				}

				ProcessType(injectableTypes, injectedTypes, typeDefinition);
			}

			foreach (var typeDefinition in moduleDefinition.Types)
			{
				if (typeDefinition.IsAbstract || typeDefinition.IsValueType || typeDefinition.IsGenericParameter || typeDefinition.ContainsGenericParameter || typeDefinition.IsPrimitive || typeDefinition.IsArray || injectedTypes.Contains(typeDefinition))
				{
					continue;
				}

				foreach (var typeInterface in typeDefinition.Interfaces)
				{
					if (injectedTypes.Contains(typeInterface.InterfaceType))
					{
						injectedTypes.Add(typeDefinition);
						break;
					}
				}

				var baseType = typeDefinition.BaseType;
				while (!injectedTypes.Contains(typeDefinition) && baseType != null && baseType.Module == typeDefinition.Module)
				{
					if (injectedTypes.Contains(baseType))
					{
						injectedTypes.Add(typeDefinition);
						break;
					}

					var resolvedBaseType = baseType.Resolve();

					if (resolvedBaseType != null && baseType is GenericInstanceType genericInstanceType && genericInstanceType.HasGenericArguments)
					{
						Dictionary<TypeReference, TypeReference> genericTypeToRealType = new Dictionary<TypeReference, TypeReference>(new TypeReferenceComprarer());

						for (int i = 0; i < genericInstanceType.GenericArguments.Count; i++)
						{
							genericTypeToRealType[resolvedBaseType.GenericParameters[i]] = genericInstanceType.GenericArguments[i];
						}

						foreach (var typeInterface in resolvedBaseType.Interfaces)
						{
							if (typeInterface.InterfaceType is GenericInstanceType genericInterface && genericInterface.HasGenericArguments)
							{
								var resolvedInterface = new GenericInstanceType(genericInterface.ElementType);

								Dictionary<int, TypeReference> realTypes = new Dictionary<int, TypeReference>();
								int index = 0;
								foreach (var genericType in genericInterface.GenericArguments)
								{
									resolvedInterface.GenericArguments.Add(genericType);

									if (genericTypeToRealType.TryGetValue(genericType, out var realType))
									{
										realTypes[index] = realType;
									}

									++index;
								}

								foreach (var realType in realTypes)
								{
									resolvedInterface.GenericArguments.RemoveAt(realType.Key);
									resolvedInterface.GenericArguments.Insert(realType.Key, realType.Value);
								}

								if (injectedTypes.Contains(resolvedInterface))
								{
									injectedTypes.Add(typeDefinition);
									break;
								}
							}
						}
					}

					if (resolvedBaseType != null)
					{
						foreach (var typeInterface in resolvedBaseType.Interfaces)
						{
							if (injectedTypes.Contains(typeInterface.InterfaceType))
							{
								injectedTypes.Add(typeDefinition);
								break;
							}
						}
					}

					baseType = resolvedBaseType.BaseType;
				}
			}

			foreach (var injectableType in injectableTypes)
			{
				var hasBaseInjectableType = injectableTypes.Any(i => i.Type.FullName == injectableType.Type.BaseType.FullName);

				var isItemController = injectableType.Type.BaseType is TypeSpecification typeSpecification &&
					(typeSpecification.ElementType?.FullName == AssemblyInjector.ItemControllerType.FullName || typeSpecification.ElementType?.FullName == AssemblyInjector.ItemsControllerType.FullName);

				if (hasBaseInjectableType || isItemController)
				{
					injectableType.InjectableBaseType = injectableType.Type.BaseType;
				}
				else if (injectableType.Type.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.InheritsFromInjectableAttributeType.FullName).FirstOrDefault() is CustomAttribute inheritsFromInjectableAttribute)
				{
					var baseType = inheritsFromInjectableAttribute.ConstructorArguments.FirstOrDefault().Value as TypeReference ?? injectableType.Type.BaseType;

					injectableType.InjectableBaseType = baseType;
				}
			}

			InjectableTypes = injectableTypes;
			InjectedTypes = injectedTypes;
		}

		protected virtual void ProcessType(List<InjectableType> injectableTypes, HashSet<TypeReference> injectedTypes, TypeDefinition typeDefinition)
		{
			InjectableType injectableType = null;

			var ownsAttributes = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.OwnsAttributeType.FullName);
			if (ownsAttributes.Any())
			{
				injectableType ??= new InjectableType(typeDefinition);

				foreach (var ownsAttribute in ownsAttributes)
				{
					var ownedType = ownsAttribute.ConstructorArguments.First().Value as TypeReference;

					injectableType.OwnedTypes.Add(ownedType);
					injectedTypes.Add(ownedType);
				}
			}

			var ownsAllAttributes = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.OwnsAllAttributeType.FullName);
			if (ownsAllAttributes.Any())
			{
				injectableType ??= new InjectableType(typeDefinition);

				foreach (var ownsAllAttribute in ownsAllAttributes)
				{
					var ownedAllType = ownsAllAttribute.ConstructorArguments.First().Value as TypeReference;

					injectableType.OwnedAllTypes.Add(ownedAllType);
					injectedTypes.Add(ownedAllType);
				}
			}

			var neededFields = typeDefinition.Fields.Where(f => f.CustomAttributes.Any(a => a.AttributeType.FullName == AssemblyInjector.NeedsAttributeType.FullName));
			if (neededFields.Any())
			{
				injectableType ??= new InjectableType(typeDefinition);

				foreach (var neededField in neededFields)
				{
					var neededType = neededField.FieldType;

					var neededAttribute = neededField.CustomAttributes.First(a => a.AttributeType.FullName == AssemblyInjector.NeedsAttributeType.FullName);
					var ignoreInitialization = (bool)(neededAttribute.ConstructorArguments.FirstOrDefault().Value ?? false);

					injectableType.NeededTypes.Add(neededField, (neededType, ignoreInitialization));
					injectedTypes.Add(neededType);
				}
			}

			bool hasIncludeTypeAttribute = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.IncludeTypeAttributeType.FullName).Any();
			if (hasIncludeTypeAttribute)
			{
				injectedTypes.Add(typeDefinition);
			}

			bool hasInheritsFromInjectableAttribute = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.InheritsFromInjectableAttributeType.FullName).Any();
			if (hasInheritsFromInjectableAttribute)
			{
				injectableType ??= new InjectableType(typeDefinition);
				injectedTypes.Add(typeDefinition);
			}

			var isControllerTypes = !typeDefinition.IsInterface && typeDefinition.Interfaces.Any(i => i.InterfaceType.FullName == AssemblyInjector.IControlledItemType.FullName || i.InterfaceType.FullName == AssemblyInjector.IItemControllerType.FullName);
			if (isControllerTypes)
			{
				injectableType ??= new InjectableType(typeDefinition);
				injectedTypes.Add(typeDefinition);
			}

			var observedTypes = typeDefinition.Interfaces
				.Where(i => i.InterfaceType is GenericInstanceType genericInstanceType &&
					genericInstanceType.HasGenericArguments &&
					genericInstanceType.ElementType.FullName == AssemblyInjector.IItemObserverGenericType.FullName)
				.Select(i => (i.InterfaceType as GenericInstanceType).GenericArguments[0]);
			if (observedTypes.Any())
			{
				if (!typeDefinition.IsInterface)
				{
					injectableType ??= new InjectableType(typeDefinition);
					injectableType.ObservedTypes.UnionWith(observedTypes);
				}
				injectedTypes.UnionWith(observedTypes);
			}

			foreach (var method in typeDefinition.Methods)
			{
				if (method.HasBody)
				{
					foreach (var instruction in method.Body.Instructions)
					{
						if (instruction.OpCode == OpCodes.Call)
						{
							var methodReference = instruction.Operand as GenericInstanceMethod;
							if (methodReference != null)
							{
								if (methodReference.DeclaringType.FullName == AssemblyInjector.InjectorType.FullName &&
									(methodReference.Name == GetInstanceMethodName || methodReference.Name == GetInstancesMethodName))
								{
									var injectedType = methodReference.GenericArguments.First();

									injectedTypes.Add(injectedType);
								}
								else if (methodReference.DeclaringType.FullName == InjectorAdvancedTypeName &&
									(methodReference.Name == AddInstanceMethodName || methodReference.Name == AddInstancesMethodName))
								{
									var injectedType = methodReference.GenericArguments.First();

									injectedTypes.Add(injectedType);
								}
							}
						}
					}
				}
			}

			if (injectableType != null)
			{
				injectableTypes.Add(injectableType);
				injectedTypes.Add(injectableType.Type);
			}
		}
	}
}