namespace Hollywood.Runtime.Observer
{
    public interface IObservable<T>
    {
        IUnsubscriber Subscribe(IObserver<T> observer);
    }
}
