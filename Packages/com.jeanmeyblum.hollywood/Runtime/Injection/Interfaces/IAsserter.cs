namespace Hollywood.Runtime
{
	/// <summary>
	/// Interface used by the injection system for assertion.
	/// </summary>
	public interface IAsserter
	{
		void IsTrue(bool condition, object message = null);
		void IsFalse(bool condition, object message = null);
		void IsNull<T>(T instance, object message = null) where T : class;
		void IsNotNull<T>(T instance, object message = null) where T : class;
		void Throw(object message);
	}
}