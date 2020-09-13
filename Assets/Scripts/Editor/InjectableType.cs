
using Mono.Cecil;
using System.Collections.Generic;

namespace Hollywood.Editor
{
	internal class InjectableType
	{
		public TypeDefinition Type;

		public HashSet<TypeReference> ownedInterfaceType = new HashSet<TypeReference>();
		public HashSet<TypeReference> ownedAllInterfaceType = new HashSet<TypeReference>();
		public Dictionary<FieldDefinition, TypeReference> neededInterfaceType = new Dictionary<FieldDefinition, TypeReference>();

		public InjectableType(TypeDefinition type)
		{
			Type = type;
		}
	}
}