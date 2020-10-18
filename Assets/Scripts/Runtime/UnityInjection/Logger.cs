namespace Hollywood.Runtime.UnityInjection
{
	public class Logger : ILogger
	{
		void ILogger.LogError(object message)
		{
			UnityEngine.Debug.LogError(message);
		}

		void ILogger.LogFatalError(object message)
		{
			UnityEngine.Debug.LogError(message);
		}

		void ILogger.LogMessage(object message)
		{
			UnityEngine.Debug.Log(message);
		}

		void ILogger.LogTrace(object message)
		{
			UnityEngine.Debug.Log(message);
		}

		void ILogger.LogWarning(object message)
		{
			UnityEngine.Debug.LogWarning(message);
		}
	}
}