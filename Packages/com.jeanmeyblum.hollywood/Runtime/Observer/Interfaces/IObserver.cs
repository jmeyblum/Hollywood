using System;

namespace Hollywood.Runtime.Observer
{
    public interface IObserver<T>
    {
        void OnReceived(T value);
    }
}
