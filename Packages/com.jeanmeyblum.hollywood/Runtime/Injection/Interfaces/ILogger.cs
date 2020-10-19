namespace Hollywood.Runtime
{
	public interface ILogger
	{
		void LogTrace(object message);
		void LogMessage(object message);
		void LogWarning(object message);
		void LogError(object message);
		void LogFatalError(object message);
	}
}