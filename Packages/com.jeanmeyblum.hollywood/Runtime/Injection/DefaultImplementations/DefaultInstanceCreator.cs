using System;

namespace Hollywood
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