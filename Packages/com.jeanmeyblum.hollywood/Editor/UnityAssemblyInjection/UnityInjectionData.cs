using Hollywood.Editor.AssemblyInjection;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Hollywood.Editor.UnityAssemblyInjection
{
	internal class UnityInjectionData : InjectionData
	{
		private readonly List<InjectableMonoBehaviour> injectableMonoBehaviours = new List<InjectableMonoBehaviour>();
		public IEnumerable<InjectableMonoBehaviour> InjectableMonoBehaviours => injectableMonoBehaviours;

		public UnityInjectionData(ModuleDefinition moduleDefinition)
			: base(moduleDefinition)
		{
		}

		protected override void ProcessType(List<InjectableType> injectableTypes, HashSet<TypeReference> injectedTypes, TypeDefinition typeDefinition)
		{
			base.ProcessType(injectableTypes, injectedTypes, typeDefinition);

			var observedMonoBehaviourAttributes = typeDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == UnityAssemblyInjector.ObservedMonoBehaviourAttributeType.FullName).FirstOrDefault();
			if (observedMonoBehaviourAttributes != null)
			{
				InjectableMonoBehaviour injectableMonoBehaviour = new InjectableMonoBehaviour(typeDefinition);

				injectableMonoBehaviours.Add(injectableMonoBehaviour);
				injectedTypes.Add(typeDefinition);
			}
		}
	}
}
