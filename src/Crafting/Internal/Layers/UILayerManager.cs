namespace Crafting.Internal.Layers
{
    using Core.Views.UI;
    using Godot;
    using Services;

    [GlobalClass]
    internal partial class UILayerManager : Node, ILayerManager
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _notificationLayer;

        public event System.Action? OverlaysCleared;

        private IUiElementsManager? _uiElements;

        public override void _Ready() => _uiElements = GameServiceProvider.Instance.GetService<IUiElementsManager>();

        /// <summary>Esc closes UI layer by layer: overlays first, then every dismissable window.</summary>
        public override void _UnhandledInput(InputEvent @event)
        {
            if (!@event.IsActionPressed("ui_cancel")) return;
            if (_uiElements?.HandleEscape() == true) GetViewport().SetInputAsHandled();
        }

        public void ShowHud(IHud hud)
        {
            if (hud is Control cHud)
                _mainLayer?.CallDeferred(Node.MethodName.AddChild, cHud);
        }

        public void ShowWindow(IWindow window)
        {
            if (window is Control cWindow)
                _windowLayer?.CallDeferred(Node.MethodName.AddChild, cWindow);
        }

        public void ShowOverlay(IPopup overlay)
        {
            if (overlay is Control cOverlay)
                _notificationLayer?.CallDeferred(Node.MethodName.AddChild, cOverlay);
        }

        public void RemoveMainElement(Control hud) => _mainLayer?.CallDeferred(Node.MethodName.RemoveChild, hud);

        public void RemoveWindowElement(Control window) => _windowLayer?.CallDeferred(Node.MethodName.RemoveChild, window);

        public void CloseAllWindows()
        {
            foreach (var child in _windowLayer?.GetChildren() ?? [])
                child.QueueFree();
        }

        public bool CloseOverlays()
        {
            bool closedAny = false;
            foreach (var child in _notificationLayer?.GetChildren() ?? [])
            {
                child.QueueFree();
                closedAny = true;
            }

            if (closedAny) OverlaysCleared?.Invoke();
            return closedAny;
        }
    }
}
