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

        public void ShowHud(IHud hud) => LayerBehavior.Show(_mainLayer, hud);

        public void ShowWindow(IWindow window) => LayerBehavior.Show(_windowLayer, window);

        // An unassigned region container degrades to free placement on the layer
        public void ShowOverlay(IPopup overlay) =>
            LayerBehavior.Show(RegionContainer(overlay.Region) ?? (Node?)_overlayLayer ?? this, overlay);

        public bool CloseOverlays()
        {
            bool closedAny = LayerBehavior.CloseOverlays(_overlayLayer, _topCenterRegion, _topRightRegion, _bottomRightRegion);
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
