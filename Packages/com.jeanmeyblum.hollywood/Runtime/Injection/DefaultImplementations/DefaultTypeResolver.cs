using Hollywood.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollywood
{
	public class DefaultTypeResolver : ITypeResolver
	{
		private Dictionary<Type, HashSet<Type>> TypesMap = new Dictionary<Type, HashSet<Type>>();
		private Dictionary<Type, HashSet<Type>> ClassTypesMap = new Dictionary<Type, HashSet<Type>>();

		public DefaultTypeResolver()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

			HashSet<Type> classTypes = new HashSet<Type>();

			foreach (var assembly in assemblies)
			{
				if (!assembly.GetCustomAttributes(typeof(__Hollywood_PostProcessedAttribute), false).Any())
				{
					continue;
				}

				var injectedTypesTypeName = string.Format(Constants.DefaultTypeResolver.TypeNameTemplate, assembly.Modules.First().Name);

				var injectedTypesType = assembly.GetType(injectedTypesTypeName, throwOnError: false);

				if (injectedTypesType != null)
				{
					var typesField = injectedTypesType.GetField(Constants.DefaultTypeResolver.MemberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

					var typesMap = (Type[][])typesField.GetValue(null);

					foreach (var types in typesMap)
					{
						for (int typeIndex = 0; typeIndex < types.Length; ++typeIndex)
						{
							Type type = types[typeIndex];
							if (!TypesMap.TryGetValue(type, out HashSet<Type> relatedType))
							{
								relatedType = new HashSet<Type>();
								TypesMap.Add(type, relatedType);
							}

							if (typeIndex > 0 && !types[0].IsInterface)
							{
								Type firstType = types[0];
								relatedType.Add(firstType);

								ClassTypesMap[firstType].Add(type);
							}
							else if (!type.IsInterface)
							{
								relatedType.Add(type);

								if (!ClassTypesMap.TryGetValue(type, out HashSet<Type> relatedInterfaceType))
								{
									relatedInterfaceType = new HashSet<Type>();
									ClassTypesMap.Add(type, relatedInterfaceType);
								}

								relatedInterfaceType.Add(type);
								classTypes.Add(type);
							}
						}
					}
				}
			}

			HashSet<Type> childs = new HashSet<Type>();

			foreach (var classType in classTypes)
			{
				childs.Clear();
				Type baseType = classType.BaseType;
				childs.Add(classType);

				while (baseType != null)
				{
					childs.Add(baseType);

					foreach (var child in childs)
					{
						if (!ClassTypesMap.TryGetValue(child, out HashSet<Type> relatedParentType))
						{
							relatedParentType = new HashSet<Type>();
							ClassTypesMap.Add(child, relatedParentType);
						}

						relatedParentType.Add(baseType);
					}

					baseType = baseType.BaseType;
				}
			}

			HashSet<Type> typesToRemove = new HashSet<Type>();
			HashSet<Type> typesToAdd = new HashSet<Type>();
			HashSet<Type> emptyTypes = new HashSet<Type>();

			foreach (var typeMap in TypesMap)
			{
				var map = typeMap.Value;

				typesToRemove.Clear();
				typesToAdd.Clear();

				foreach (var type in map)
				{
					if (type.IsInterface)
					{
						typesToRemove.Add(type);

						FindTypeToAddRecursively(type, typesToAdd);
					}
				}

				map.ExceptWith(typesToRemove);
				map.UnionWith(typesToAdd);

				if (!map.Any())
				{
					emptyTypes.Add(typeMap.Key);
				}
			}

			foreach (var emptyType in emptyTypes)
			{
				TypesMap.Remove(emptyType);
			}

			void FindTypeToAddRecursively(Type type, HashSet<Type> typesToAdd)
			{
				if (TypesMap.TryGetValue(type, out var types))
				{
					foreach (var typeToAdd in types)
					{
						if (typeToAdd.IsInterface)
						{
							FindTypeToAddRecursively(typeToAdd, typesToAdd);
						}
						else
						{
							typesToAdd.Add(typeToAdd);
						}
					}
				}
			}
		}

		Type ITypeResolver.Get<T>()
		{
			Assert.IsTrue(TypesMap.TryGetValue(typeof(T), out var types) && types.Count == 1, $"Issue resolving type for {typeof(T)}");

			return TypesMap[typeof(T)].First();
		}

		IEnumerable<Type> ITypeResolver.GetAll<T>()
		{
			Assert.IsTrue(TypesMap.TryGetValue(typeof(T), out var types) && types.Count > 0, $"Issue resolving types for {typeof(T)}");

			return TypesMap[typeof(T)];
		}

		IEnumerable<Type> ITypeResolver.GetAssignableTypes(Type type)
		{
			Assert.IsTrue(ClassTypesMap.TryGetValue(type, out var types) && types.Count > 0, $"Issue getting assignable types for {type}");

			return ClassTypesMap[type];
		}

		void ITypeResolver.Reset()
		{ }
	}
}