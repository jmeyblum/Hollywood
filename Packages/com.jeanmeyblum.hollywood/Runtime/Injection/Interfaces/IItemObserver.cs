namespace Hollywood.Runtime
{
    /// <summary>
    /// Interface that can be used on an injected system to be notified of
    /// creation and destruction of items of type T.
    /// This is called automatically through the injection system.
    /// </summary>
    /// <typeparam name="T">Type of item to observe</typeparam>
    public interface IItemObserver<T> where T: class
    {
        /// <summary>
        /// Called when an item of type T is created.
        /// </summary>
        /// <param name="item"></param>
        void OnItemCreated(T item);

		/// <summary>
		/// Called when an item of type T is destroyed.
		/// </summary>
		/// <param name="item"></param>
		void OnItemDestroyed(T item);
    }
}