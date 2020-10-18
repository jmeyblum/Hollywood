using System.Diagnostics;

namespace Hollywood.Runtime
{
	public static class Log
	{
		public const string LoggerDefineSymbol = "HOLLYWOOD_LOG";
		public static ILogger Logger;

		[Conditional(LoggerDefineSymbol)]
		public static void LogTrace(object message)
		{
			Logger?.LogTrace(message);
		}

		[Conditional(LoggerDefineSymbol)]
		public static void LogMessage(object message)
		{
			Logger?.LogMessage(message);
		}

		[Conditional(LoggerDefineSymbol)]
		public static void LogWarning(object message)
		{
			Logger?.LogWarning(message);
		}

		[Conditional(LoggerDefineSymbol)]
		public static void LogError(object message)
		{
			Logger?.LogError(message);
		}

		[Conditional(LoggerDefineSymbol)]
		public static void LogFatalError(object message)
		{
			Logger?.LogFatalError(message);
		}
	}
}