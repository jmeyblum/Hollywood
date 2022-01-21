
namespace Hollywood.Unity
{
	[IncludeType]
	public class Asserter : IAsserter
	{
		void IAsserter.Throw(object message)
		{
			UnityEngine.Debug.LogAssertion(message);
		}

		void IAsserter.IsFalse(bool condition, object message)
		{
			UnityEngine.Assertions.Assert.IsFalse(condition, $"{message}");
		}

		void IAsserter.IsNotNull<T>(T instance, object message)
		{
			UnityEngine.Assertions.Assert.IsNotNull(instance, $"{message}");
		}

		void IAsserter.IsNull<T>(T instance, object message)
		{
			UnityEngine.Assertions.Assert.IsNull(instance, $"{message}");
		}

		void IAsserter.IsTrue(bool condition, object message)
		{
			UnityEngine.Assertions.Assert.IsTrue(condition, $"{message}");
		}
	}
}