namespace Core.Views.UI
{
    using System;

    /// <summary>
    /// The single creation point of top-level UI (HUDs and windows). Projects register scene
    /// factories at bootstrap; every open creates a FRESH instance with services injected,
    /// every close frees it. Persistent UI state (window positions etc.) belongs in dedicated
    /// services like <see cref="IUIWindowPositionStorage"/>, not in kept-alive nodes.
    /// </summary>
    public interface IUiElementsManager
    {
        void Subscribe(ILayerManager layers);

        IHud ChangeHud(Type hudType);

        /// <summary>Toggle: opening an already open window closes it instead.</summary>
        IWindow OpenWindow(Type windowType);

        /// <summary>Returns the already open window if there is one, otherwise opens a fresh one. Never closes.</summary>
        IWindow GetOrOpenWindow(Type windowType);

        bool RegisterHudFactory(Type hudType, Func<IHud> factory);
        bool RegisterWindowFactory(Type windowType, Func<IWindow> factory);
    }
}
