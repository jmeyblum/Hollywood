using Hollywood.Runtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollywood.Runtime
{
	// TODO: add settings to have a list of ignored assemblies

	public class DefaultTypeResolver : ITypeResolver
	{
		private static readonly Type IIgnoreAttributeType = typeof(IgnoreTypeAttribute);
		private Dictionary<Type, HashSet<Type>> InterfaceToTypes = new Dictionary<Type, HashSet<Type>>();

		public DefaultTypeResolver()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

			foreach (var assembly in assemblies)
			{
				var injectedInterfacesTypeName = string.Format(Constants.DefaultTypeResolver.TypeNameTemplate, assembly.Modules.First().Name);

				var injectedInterfacesType = assembly.GetType(injectedInterfacesTypeName, throwOnError: false);

				if (injectedInterfacesType != null)
				{
					var interfaceNamesField = injectedInterfacesType.GetField(Constants.DefaultTypeResolver.MemberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

					var interfaceNames = (string[])interfaceNamesField.GetValue(null);

					foreach (var interfaceName in interfaceNames)
					{
						Type interfaceType = Type.GetType(interfaceName);

						if (!InterfaceToTypes.ContainsKey(interfaceType))
						{
							InterfaceToTypes.Add(interfaceType, new HashSet<Type>());
						}
					}
				}
			}

			var types = assemblies.SelectMany(a => a.GetTypes())
				.Where(t =>
				t.IsClass &&
				!t.IsAbstract &&
				!t.IsArray &&
				!t.IsPrimitive &&
				!t.IsEnum &&
				!t.IsGenericParameter &&
				!t.ContainsGenericParameters &&
				t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null);

			foreach (var type in types)
			{
				var interfaces = type.GetInterfaces();

				bool isTypeIgnored = false;
				foreach (var typeInterfaces in interfaces)
				{
					if(typeInterfaces == IIgnoreAttributeType)
					{
						isTypeIgnored = true;
						break;
					}
				}

				if(isTypeIgnored)
				{
					continue;
				}

				foreach (var typeInterfaces in interfaces)
				{
					if (InterfaceToTypes.TryGetValue(typeInterfaces, out var interfaceTypes))
					{
						interfaceTypes.Add(type);
					}
				}
			}
		}

		Type ITypeResolver.Get<T>()
		{
			Assert.IsTrue(InterfaceToTypes.TryGetValue(typeof(T), out var types) && types.Count == 1);

			return InterfaceToTypes[typeof(T)].First();
		}

		IEnumerable<Type> ITypeResolver.GetAll<T>()
		{
			Assert.IsTrue(InterfaceToTypes.TryGetValue(typeof(T), out var types) && types.Count > 0);

			return InterfaceToTypes[typeof(T)];
		}

		void ITypeResolver.Reset()
		{ }
	}
}