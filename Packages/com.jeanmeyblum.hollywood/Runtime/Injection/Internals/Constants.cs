namespace Hollywood.Runtime.Internal
{
	public static class Constants
	{
		public static class DefaultTypeResolver
		{
			public const string AssemblyNamePrefix = "__Hollywood";
			public const string TypeName = "__InjectedTypes";
			public static readonly string AssemblyNameTemplate = $"{AssemblyNamePrefix}.{{0}}";
			public static readonly string TypeNameTemplate = $"{AssemblyNamePrefix}.{{0}}.{TypeName}";
			public const string MemberName = "__types";
		}

		public const string InstanceInitializerMethodName = ".ctor";
		public const string TypeInitializerMethodName = ".cctor";
	}
}