namespace Hollywood.Runtime
{
    public interface IItemObserver<T> where T: class
    {
        void OnItemCreated(T item);

        void OnItemDestroyed(T item);
    }
}