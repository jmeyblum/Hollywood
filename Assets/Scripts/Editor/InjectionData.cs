
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Hollywood.Editor
{
    internal class InjectionData
	{
		public IEnumerable<InjectableType> InjectableTypes { get; private set; }
		public IEnumerable<InjectedInterface> InjectedInterfaces { get; private set; }

		public InjectionData(ModuleDefinition moduleDefinition)
		{
			var injectableTypes = new HashSet<InjectableType>();
			var injectedInterfaces = new HashSet<InjectedInterface>();			

			foreach (var typeDefinition in moduleDefinition.Types)
			{
				InjectableType injectableType = null;

				var ownsAttributes = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.OwnsAttributeType.FullName);
				if (ownsAttributes.Any())
				{
					injectableType = injectableType ?? new InjectableType(typeDefinition);

					foreach (var ownsAttribute in ownsAttributes)
					{
						var ownedType = ownsAttribute.ConstructorArguments.First().Value as TypeReference;

						injectableType.ownedInterfaceType.Add(ownedType);
						injectedInterfaces.Add(new InjectedInterface(ownedType));
					}
				}

				var ownsAllAttributes = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == AssemblyInjector.OwnsAllAttributeType.FullName);
				if (ownsAllAttributes.Any())
				{
					injectableType = injectableType ?? new InjectableType(typeDefinition);

					foreach (var ownsAllAttribute in ownsAllAttributes)
					{
						var ownedAllType = ownsAllAttribute.ConstructorArguments.First().Value as TypeReference;

						injectableType.ownedAllInterfaceType.Add(ownedAllType);
						injectedInterfaces.Add(new InjectedInterface(ownedAllType));
					}
				}

				var neededFields = typeDefinition.Fields.Where(f => f.CustomAttributes.Any(a => a.AttributeType.FullName == AssemblyInjector.NeedsAttributeType.FullName));
				if (neededFields.Any())
				{
					injectableType = injectableType ?? new InjectableType(typeDefinition);

					foreach (var neededField in neededFields)
					{
						var interfaceType = neededField.FieldType;

						injectableType.neededInterfaceType.Add(neededField, interfaceType);
						injectedInterfaces.Add(new InjectedInterface(interfaceType));
					}
				}

				if (injectableType != null)
				{
					injectableTypes.Add(injectableType);
				}
			}

			InjectableTypes = injectableTypes;
			InjectedInterfaces = injectedInterfaces;
		}
	}
}