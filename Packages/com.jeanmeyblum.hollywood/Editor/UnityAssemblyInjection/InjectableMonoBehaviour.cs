using Mono.Cecil;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	internal class InjectableMonoBehaviour
	{
		public TypeDefinition Type;
		public TypeReference MonoBehaviourBaseType;

		public InjectableMonoBehaviour(TypeDefinition type)
		{
			Type = type;
		}
	}
}
