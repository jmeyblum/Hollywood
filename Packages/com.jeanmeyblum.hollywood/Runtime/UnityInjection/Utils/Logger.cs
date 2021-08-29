namespace Hollywood.Runtime.UnityInjection
{
	[IncludeType]
	public class Logger : ILogger
	{
		public LogLevel LogLevel { get; set; } = LogLevel.Error;

		void ILogger.LogError(object message)
		{
			if (LogLevel.HasFlag(LogLevel.ErrorOnly) == true)
			{
				UnityEngine.Debug.LogError(message);
			}
		}

		void ILogger.LogFatalError(object message)
		{
			if (LogLevel.HasFlag(LogLevel.FatalErrorOnly) == true)
			{
				UnityEngine.Debug.LogError(message);
			}
		}

		void ILogger.LogMessage(object message)
		{
			if (LogLevel.HasFlag(LogLevel.MessageOnly) == true)
			{
				UnityEngine.Debug.Log(message);
			}
		}

		void ILogger.LogTrace(object message)
		{
			if (LogLevel.HasFlag(LogLevel.TraceOnly) == true)
			{
				UnityEngine.Debug.Log(message);
			}
		}

		void ILogger.LogWarning(object message)
		{
			if (LogLevel.HasFlag(LogLevel.WarningOnly) == true)
			{
				UnityEngine.Debug.LogWarning(message);
			}
		}
	}
}