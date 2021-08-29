namespace Hollywood.Runtime
{
	/// <summary>
	/// Interface used by the injection system to log messages.
	/// </summary>
	public interface ILogger
	{
		LogLevel LogLevel { get; }
		void LogTrace(object message);
		void LogMessage(object message);
		void LogWarning(object message);
		void LogError(object message);
		void LogFatalError(object message);
	}
}