using System;

namespace Hollywood.Runtime
{
	public interface IInstanceCreator
	{
		object Create(Type type);
		void Reset();
	}
}