
namespace Hollywood.Observer
{
	/// <summary>
	/// Interface used to subscribe to observers for which this observable will sent event of type T.
	/// This can be used to communicate between systems without creating unwanted dependencies between them.
	/// The implementation of Subscribe can be simplified by delegating it to an ObservableHandler which can
	/// be owned by a system implementing this interface.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IObservable<T>
	{
		IUnsubscriber Subscribe(IObserver<T> observer);
	}
}
