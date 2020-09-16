
using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Hollywood.Editor
{

	public class InjectedInterface : IEquatable<InjectedInterface>
	{
		public TypeReference Type;

		public InjectedInterface(TypeReference type)
		{
			Type = type;
		}

		public override string ToString()
		{
			return $"{Type.FullName}, {Type.Scope}";
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as InjectedInterface);
		}

		public bool Equals(InjectedInterface other)
		{
			return other != null &&
				   ToString() == other.ToString();
		}

		public override int GetHashCode()
		{
			return 2049151605 + ToString().GetHashCode();
		}

		public static bool operator ==(InjectedInterface left, InjectedInterface right)
		{
			return EqualityComparer<InjectedInterface>.Default.Equals(left, right);
		}

		public static bool operator !=(InjectedInterface left, InjectedInterface right)
		{
			return !(left == right);
		}
	}
}