
namespace Hollywood.Observer
{
	/// <summary>
	/// Interface that can be implemented by a system which wants to listen to event of type T.
	/// The IObserver needs to be subscribed to an IObservable.
	/// This can be used to communicate between systems without creating unwanted dependencies between them.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IObserver<T>
	{
		void OnReceived(T value);
	}
}
