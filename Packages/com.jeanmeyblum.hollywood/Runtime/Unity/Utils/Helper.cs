﻿using System;
using UnityEngine;

namespace Hollywood.Unity
{
	public static class Helper
	{
		public const string UnityAutoSetupDefineSymbol = "HOLLYWOOD_UNITY_AUTO_SETUP";

		/// <summary>
		/// Initialize Injector with a default InjectionContext and Unity specific implementation for Logger and Asserter.
		/// </summary>
		public static void InitializeHollywoodWithDefaultForUnity()
		{
			Assert.IsNull(Injector.InjectionContext);

			SetupUnitySpecificImplementations();

			Injector.InjectionContext = Hollywood.Helper.CreateDefaultInjectionContext();

			Injector.Advanced.AddExternalInstance<ILogger>(Log.Logger);
			Injector.Advanced.AddExternalInstance<IAsserter>(Assert.Asserter);
		}

		/// <summary>
		/// Setup default Unity specific implementation for Logger and Asserter
		/// </summary>
		public static void SetupUnitySpecificImplementations()
		{
			SetupUnityLogger();
			SetupUnityAsserter();
		}

		/// <summary>
		/// Setup default Unity specific implementation for Logger
		/// </summary>
		public static void SetupUnityLogger()
		{
			var logger = new Logger();

#if HOLLYWOOD_UNITY_LOG_TRACE
			logger.LogLevel = LogLevel.Trace;
#elif HOLLYWOOD_UNITY_LOG_MESSAGE
			logger.LogLevel = LogLevel.Message;
#elif HOLLYWOOD_UNITY_LOG_WARNING
			logger.LogLevel = LogLevel.Warning;			
#elif HOLLYWOOD_UNITY_LOG_ERROR
			logger.LogLevel = LogLevel.Error;
#elif HOLLYWOOD_UNITY_LOG_FATAL_ERROR
			logger.LogLevel = LogLevel.FatalError;
#elif HOLLYWOOD_UNITY_LOG_NONE
			logger.LogLevel = LogLevel.None;
#endif
			Log.Logger = logger;
		}

		/// <summary>
		/// Setup default Unity specific implementation for Asserter
		/// </summary>
		public static void SetupUnityAsserter()
		{
			Assert.Asserter = new Asserter();
		}

#if HOLLYWOOD_UNITY_AUTO_SETUP
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void AutoSetupHollywoodWithDefaultForUnity()
		{
			InitializeHollywoodWithDefaultForUnity();
		}
#endif

#if UNITY_EDITOR && HOLLYWOOD_UNITY_AUTO_SETUP
		[UnityEditor.InitializeOnLoadMethod]
		static void OnInitializeOnLoad()
		{
			UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange playModeStateChange)
		{
			switch (playModeStateChange)
			{
				case UnityEditor.PlayModeStateChange.ExitingPlayMode:
					Injector.Dispose();
					break;
			}
		}
#endif

		public static class Internal
		{
#if UNITY_EDITOR
			private static bool TransitioningFromToPlayMode = false;

			[UnityEditor.InitializeOnLoadMethod]
			static void OnInitializeOnLoad()
			{
				UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
				UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			}

			private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange playModeStateChange)
			{
				switch (playModeStateChange)
				{
					case UnityEditor.PlayModeStateChange.ExitingPlayMode:
					case UnityEditor.PlayModeStateChange.ExitingEditMode:
						TransitioningFromToPlayMode = true;
						break;
					default:
						TransitioningFromToPlayMode = false;
						break;
				}
			}
#endif

			public static void NotifyMonoBehaviourCreation(MonoBehaviour monoBehaviour)
			{
#if UNITY_EDITOR
				if (!TransitioningFromToPlayMode)
#endif
				{
					Injector.Advanced.NotifyItemCreation(monoBehaviour);
				}
			}

			public static void NotifyMonoBehaviourDestruction(MonoBehaviour monoBehaviour)
			{
#if UNITY_EDITOR
				if (!TransitioningFromToPlayMode)
#endif
				{
					Injector.Advanced.NotifyItemDestruction(monoBehaviour);
				}
			}
		}
	}
}