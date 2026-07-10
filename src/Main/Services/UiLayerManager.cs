namespace LastBreath.Services
{
    using System;
    using Core.Views.UI;
    using Godot;

    public partial class UiLayerManager : Node, ILayerManager
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _overlayLayer;

        public event Action? OverlaysCleared;

        // Region containers under the overlay layer (VBoxContainer nodes anchored in the scene):
        // the layer owns geometry and stacking, popups only declare the region they want.
        [Export] private Control? _topCenterRegion, _topRightRegion, _bottomRightRegion;

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
            if (overlay is not Control cOverlay) return;
            // An unassigned region container degrades to free placement on the layer
            Node target = RegionContainer(overlay.Region) ?? (Node?)_overlayLayer ?? this;
            target.CallDeferred(Node.MethodName.AddChild, cOverlay);
        }

        public void CloseAllWindows()
        {
            foreach (var child in _windowLayer?.GetChildren() ?? [])
                child.QueueFree();
        }

        public bool CloseOverlays()
        {
            bool closedAny = false;
            foreach (var child in _overlayLayer?.GetChildren() ?? [])
            {
                // Region containers are scene furniture: clear their content, keep the container
                if (child == _topCenterRegion || child == _topRightRegion || child == _bottomRightRegion)
                {
                    foreach (var popup in child.GetChildren())
                    {
                        popup.QueueFree();
                        closedAny = true;
                    }

                    continue;
                }

                child.QueueFree();
                closedAny = true;
            }

            if (closedAny) OverlaysCleared?.Invoke();
            return closedAny;
        }

        private Control? RegionContainer(OverlayRegion region) => region switch
        {
            OverlayRegion.TopCenter => _topCenterRegion,
            OverlayRegion.TopRight => _topRightRegion,
            OverlayRegion.BottomRight => _bottomRightRegion,
            _ => null // Cursor: free placement, the popup positions itself via UiPlacement
        };
    }
}
