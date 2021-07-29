using System.Diagnostics;

namespace Hollywood.Runtime
{
	public static class Assert
	{
		public const string AssertDefineSymbol = "HOLLYWOOD_ASSERT";
		public static IAsserter Asserter;

		[Conditional(AssertDefineSymbol)]
		public static void IsTrue(bool condition, object message = null)
		{
			Asserter?.IsTrue(condition, message);
		}

		[Conditional(AssertDefineSymbol)]
		public static void IsFalse(bool condition, object message = null)
		{
			Asserter?.IsFalse(condition, message);
		}

		[Conditional(AssertDefineSymbol)]
		public static void IsNull<T>(T instance, object message = null) where T : class
		{
			Asserter?.IsNull(instance, message);
		}

		[Conditional(AssertDefineSymbol)]
		public static void IsNotNull<T>(T instance, object message = null) where T : class
		{
			Asserter?.IsNotNull(instance, message);
		}

		[Conditional(AssertDefineSymbol)]
		public static void Throw(object message)
		{
			Asserter?.Throw(message);
		}
	}
}