namespace Hollywood.Runtime
{
	/// <summary>
	/// Interface that can be implemented on system available to injection.
	/// This can be used when injecting dynamically at runtime.
	/// </summary>
	public interface IResolvable
	{
		void Resolve();
	}
}
