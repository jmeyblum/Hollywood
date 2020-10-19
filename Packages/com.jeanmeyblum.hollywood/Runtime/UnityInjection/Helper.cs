using UnityEngine;

namespace Hollywood.Runtime.UnityInjection
{
	public static class Helper
	{
		public const string UnityAutoSetupDefineSymbol = "HOLLYWOOD_UNITY_AUTO_SETUP";

		/// <summary>
		/// Initialize Injector with a default InjectionContext and Unity specific implementation for Logger and Asserter.
		/// </summary>
		public static void InitializeHollywoodWithDefaultForUnity()
		{
			UnityEngine.Debug.Log("Yo" + Injector.InjectionContext);

			Assert.IsNull(Injector.InjectionContext);

			SetupUnitySpecificImplementations();

			Injector.InjectionContext = Hollywood.Runtime.Helper.CreateDefaultInjectionContext();
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
			Log.Logger = new Logger();
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
					Injector.Reset();
					Injector.InjectionContext = null;
					break;
			}
		}
#endif

	}
}