namespace Core.Views.UI
{
    /// <summary>
    /// Project-agnostic contract of the canvas-layer host with the three UI layers:
    /// Hud (persistent), Window (temporary complex screens), Overlay (tooltips, popups,
    /// notifications). Each project keeps its own node implementation; the shared
    /// <see cref="IUiElementsManager"/> only talks to this interface.
    /// </summary>
    public interface ILayerManager
    {
        /// <summary>Raised after the Overlay layer was force-cleared (Esc) — queues must drop their backlog.</summary>
        event System.Action? OverlaysCleared;

        void ShowHud(IHud hud);
        void ShowWindow(IWindow window);
        void ShowOverlay(IPopup overlay);
        void CloseAllWindows();

        /// <summary>Frees everything on the Overlay layer; true when something was actually closed.</summary>
        bool CloseOverlays();
    }
}
