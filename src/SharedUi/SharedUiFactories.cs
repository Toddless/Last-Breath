namespace SharedUi
{
    using Core.Data;
    using Core.Views.UI;

    /// <summary>
    /// The shared UI catalog's registrations: one call every project's bootstrap makes, so an element
    /// that more than one module opens exists wherever it is opened.
    /// <para>The catalog is mounted into each project by a link and therefore compiles into each of
    /// their assemblies. Compose through the contracts (<see cref="IPickerPopup"/>); tests must not
    /// name SharedUi types, or the copies collide at the use site.</para>
    /// </summary>
    public static class SharedUiFactories
    {
        public static void AddSharedUiFactories(this IGameServiceProvider provider)
        {
            var uiElementManager = provider.GetService<IUiElementsManager>();
            uiElementManager.RegisterPopupFactory(typeof(IPickerPopup), () => ResourcePickerPopup.Initialize().Instantiate<ResourcePickerPopup>());
        }
    }
}
