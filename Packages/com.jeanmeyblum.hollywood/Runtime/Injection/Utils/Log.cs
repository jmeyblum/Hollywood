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
			if (Logger?.LogLevel.HasFlag(LogLevel.TraceOnly) == true)
			{
				Logger?.LogTrace(message);
			}
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogMessage(object message)
		{
			if (Logger?.LogLevel.HasFlag(LogLevel.MessageOnly) == true)
			{
				Logger?.LogMessage(message);
			}
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogWarning(object message)
		{
			if (Logger?.LogLevel.HasFlag(LogLevel.WarningOnly) == true)
			{
				Logger?.LogWarning(message);
			}
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogError(object message)
		{
			if (Logger?.LogLevel.HasFlag(LogLevel.ErrorOnly) == true)
			{
				Logger?.LogError(message);
			}
		}

		[Conditional(LoggerDefineSymbol)]
		internal static void LogFatalError(object message)
		{
			if (Logger?.LogLevel.HasFlag(LogLevel.FatalErrorOnly) == true)
			{
				Logger?.LogFatalError(message);
			}
		}
	}
}