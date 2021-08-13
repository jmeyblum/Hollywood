namespace Hollywood.Runtime
{
	/// <summary>
	/// Interface which allows to define a class as a module.
	/// A module can't have any dependency and serves to group related owned systems together.
	/// A module can own other modules which will be treated as if they are on the same hierarchy
	/// level as the owning module.
	/// </summary>
	public interface IModule
	{ }
}