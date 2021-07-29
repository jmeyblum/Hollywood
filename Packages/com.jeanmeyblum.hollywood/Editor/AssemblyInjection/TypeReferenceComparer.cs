using Mono.Cecil;
using System.Collections.Generic;

namespace Hollywood.Editor.AssemblyInjection
{
	public class TypeReferenceComprarer : IEqualityComparer<TypeReference>
	{
		public static TypeReferenceComprarer Default => new TypeReferenceComprarer();

		public bool Equals(TypeReference x, TypeReference y)
		{
			return TypeReferenceUtils.Equals(x, y);
		}

		public int GetHashCode(TypeReference obj)
		{
			return TypeReferenceUtils.GetHashCode(obj);
		}
	}
}