namespace Core.Interfaces.UI
{
    using Godot;

    /// <summary>
    /// Project-agnostic contract of the canvas-layer host: where HUDs, windows and
    /// notifications land. Each project keeps its own node implementation; the shared
    /// <see cref="IUiElementsManager"/> only talks to this interface.
    /// </summary>
    public interface ILayerManager
    {
        void ShowHud(IHud hud);
        void ShowWindow(IWindow window);
        void ShowNotification(Control notification);
        void CloseAllWindows();
    }
}
