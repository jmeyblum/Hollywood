
namespace Hollywood
{
	public enum LogLevel
	{
		None = 0,

		FatalErrorOnly = 1 << 0,
		ErrorOnly = 1 << 1,
		WarningOnly = 1 << 2,
		MessageOnly = 1 << 3,
		TraceOnly = 1 << 4,

		FatalError = FatalErrorOnly,
		Error = ErrorOnly | FatalErrorOnly,
		Warning = WarningOnly | ErrorOnly | FatalErrorOnly,
		Message = MessageOnly | WarningOnly | ErrorOnly | FatalErrorOnly,
		Trace = TraceOnly | MessageOnly | WarningOnly | ErrorOnly | FatalErrorOnly,

		All = Trace,
	}
}
