namespace Core.Views.UI
{
    using System;

    /// <summary>
    /// The single creation point of top-level UI (HUDs, windows, overlay popups). Projects
    /// register scene factories at bootstrap; every open creates a FRESH instance with services
    /// injected, every close frees it. Persistent UI state (window positions etc.) belongs in
    /// dedicated services like <see cref="IUIWindowPositionStorage"/>, not in kept-alive nodes.
    /// </summary>
    public interface IUiElementsManager
    {
        void Subscribe(ILayerManager layers);

        IHud ChangeHud(Type hudType);

        /// <summary>Hotkey semantics: opens the window, or closes it when it is already open.</summary>
        IWindow ToggleWindow(Type windowType);

        /// <summary>Returns the already open window if there is one, otherwise opens a fresh one. Never closes.</summary>
        IWindow OpenWindow(Type windowType);

        /// <summary>
        /// Always a fresh instance; the previous popup of the SAME type is closed first,
        /// popups of different types coexist (an item tooltip next to a notification).
        /// </summary>
        IPopup ShowPopup(Type popupType);

        /// <summary>
        /// Esc, layer by layer: the first press clears the Overlay layer, the next one closes
        /// every dismissable window. True when something was closed (the input is consumed).
        /// </summary>
        bool HandleEscape();

        bool RegisterHudFactory(Type hudType, Func<IHud> factory);
        bool RegisterWindowFactory(Type windowType, Func<IWindow> factory);
        bool RegisterPopupFactory(Type popupType, Func<IPopup> factory);
    }
}
