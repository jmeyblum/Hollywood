using System;
using System.Collections.Generic;

namespace Hollywood.Runtime.Observer
{
	public class ObservableHandler<T> : IObservable<T> where T : class
	{
		[Needs]
		private ILogger _logger;

		private struct Unsubscriber : IUnsubscriber
		{
			private ObservableHandler<T> Handler;
			private IObserver<T> Observer;

			public Unsubscriber(ObservableHandler<T> handler, IObserver<T> observer)
			{
				Handler = handler;
				Observer = observer;
			}

			void IUnsubscriber.Unsubscribe()
			{
				Handler.Unsubscribe(Observer);
			}
		}

		private bool Sending = false;
		private readonly List<IObserver<T>> Observers = new List<IObserver<T>>();
		private readonly List<IObserver<T>> ObserversToAdd = new List<IObserver<T>>();
		private readonly List<IObserver<T>> ObserversToRemove = new List<IObserver<T>>();

		public IUnsubscriber Subscribe(IObserver<T> observer)
		{
			if (!Sending)
			{
				if (!Observers.Contains(observer))
				{
					Observers.Add(observer);
				}
			}
			else
			{
				if (ObserversToRemove.Contains(observer))
				{
					ObserversToRemove.Remove(observer);
				}

				if (!ObserversToAdd.Contains(observer))
				{
					ObserversToAdd.Add(observer);
				}
			}

			return new Unsubscriber(this, observer);
		}

		private void UpdateObservers()
		{
			if (!Sending)
			{
				foreach (var observer in ObserversToAdd)
				{
					Observers.Add(observer);
				}
				ObserversToAdd.Clear();

				foreach (var observer in ObserversToRemove)
				{
					Observers.Add(observer);
				}
				ObserversToRemove.Clear();
			}
		}

		public void Send(T value)
		{
			UpdateObservers();
			Sending = true;

			foreach (var observer in Observers)
			{
				try
				{
					observer.OnReceived(value);
				}
				catch (Exception e)
				{
					_logger.LogError(e);
				}
			}

			Sending = false;
		}

		private void Unsubscribe(IObserver<T> observer)
		{
			if (!Sending)
			{
				Observers.Remove(observer);
			}
			else
			{
				if (ObserversToAdd.Contains(observer))
				{
					ObserversToAdd.Remove(observer);
				}

				if (!ObserversToRemove.Contains(observer))
				{
					ObserversToRemove.Add(observer);
				}
			}
		}
	}
}
