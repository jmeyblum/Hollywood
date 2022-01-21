using System;

namespace Hollywood.Editor.AssemblyInjection
{
	public class AssemblyAlreadyInjectedException : InvalidOperationException
	{
		public AssemblyAlreadyInjectedException(string message) : base(message)
		{

		}
	}
}
