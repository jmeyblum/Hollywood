
namespace Hollywood
{
	public static class Helper
	{
		/// <summary>
		/// Creates a default InjectionContext configured with a default TypeResolver and and default InstanceCreator.
		/// </summary>
		/// <returns></returns>
		public static DefaultInjectionContext CreateDefaultInjectionContext()
		{
			return new DefaultInjectionContext(new DefaultTypeResolver(), new DefaultInstanceCreator());
		}

		/// <summary>
		/// Initialize Injector with a default InjectionContext.
		/// </summary>
		public static void InitializeHollywoodWithDefault()
		{
			Assert.IsNull(Injector.InjectionContext);

			Injector.InjectionContext = CreateDefaultInjectionContext();
		}
	}
}