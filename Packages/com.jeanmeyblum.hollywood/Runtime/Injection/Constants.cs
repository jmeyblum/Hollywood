﻿namespace Hollywood.Runtime.Internal
{
	public static class Constants
	{
		public static class ReflectionTypeResolver
		{
			public const string AssemblyNamePrefix = "__Hollywood";
			public const string TypeName = "__InjectedInterfaces";
			public static readonly string AssemblyNameTemplate = $"{AssemblyNamePrefix}.{{0}}";
			public static readonly string TypeNameTemplate = $"{AssemblyNamePrefix}.{{0}}.{TypeName}";
			public const string MemberName = "__interfaceNames";
		}

		public const string InstanceInitializerMethodName = ".ctor";
		public const string TypeInitializerMethodName = ".cctor";
	}
}