using Mono.Cecil;
using System.Collections.Generic;

namespace Hollywood.Editor.AssemblyInjection
{
	public static class TypeReferenceUtils
	{
		public static bool Equals(TypeReference x, TypeReference y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x is null || y is null)
			{
				return false;
			}

			return x.FullName == y.FullName &&
				x.Scope.Name == y.Scope.Name;
		}

		public static int GetHashCode(TypeReference obj)
		{
			if (obj is null)
			{
				return 0;
			}

			int hashCode = 563624491;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(obj.FullName);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(obj.Scope.Name);
			return hashCode;
		}
	}
}