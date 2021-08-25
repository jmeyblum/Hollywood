using System.Diagnostics;

namespace Hollywood.Runtime
{
	public static class Log
	{
		private const string LoggerDefineSymbol = "HOLLYWOOD_LOG";
		public static ILogger Logger;

		[Conditional(LoggerDefineSymbol)]
		internal static void LogTrace(object message)
		{
			Logger?.LogTrace(message);
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogMessage(object message)
		{
			Logger?.LogMessage(message);
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogWarning(object message)
		{
			Logger?.LogWarning(message);
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogError(object message)
		{
			Logger?.LogError(message);
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogFatalError(object message)
		{
			Logger?.LogFatalError(message);
		}
	}
}