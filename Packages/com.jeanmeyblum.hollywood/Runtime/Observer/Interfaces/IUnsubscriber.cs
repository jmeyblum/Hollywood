
namespace Hollywood.Observer
{
	/// <summary>
	/// Object resulted from subscribing to an IObservable which must can used
	/// to unsubscribe.
	/// </summary>
	public interface IUnsubscriber
	{
		void Unsubscribe();
	}
}
