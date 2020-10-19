using System;

namespace Hollywood.Runtime
{
	public class DefaultInstanceCreator : IInstanceCreator
	{
		object IInstanceCreator.Create(Type type)
		{
			return Activator.CreateInstance(type, nonPublic: true);
		}

		void IInstanceCreator.Reset()
		{ }
	}
}