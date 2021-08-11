
using Mono.Cecil;
using System.Collections.Generic;

namespace Hollywood.Editor.AssemblyInjection
{
	internal class InjectableType
	{
		public TypeDefinition Type;

		public HashSet<TypeReference> OwnedTypes = new HashSet<TypeReference>(TypeReferenceComprarer.Default);
		public HashSet<TypeReference> OwnedAllTypes = new HashSet<TypeReference>(TypeReferenceComprarer.Default);
		public Dictionary<FieldDefinition, (TypeReference, bool)> NeededTypes = new Dictionary<FieldDefinition, (TypeReference, bool)>();
		public HashSet<TypeReference> ObservedTypes = new HashSet<TypeReference>(TypeReferenceComprarer.Default);

		public TypeReference InjectableBaseType;

		public InjectableType(TypeDefinition type)
		{
			Type = type;
		}
	}
}