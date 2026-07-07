namespace Battle.Source.UIElements
{
    using Core.Data;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Reference card for a clicked keyword ({@Effect_X} link in a description). Overlay layer,
    /// Pinned lifetime: the player closes it with the ✕ button, Esc or a click outside; clicking
    /// another keyword shows a fresh card instead (ShowPopup replaces same-type popups).
    /// Shown next to the cursor, clamped to the viewport. The root ignores mouse input so the
    /// rest of the UI stays clickable; links inside the card are not attached — no recursion.
    /// </summary>
    [GlobalClass]
    public partial class KeywordTooltipPopup : Control, IPopup
    {
        private const string UID = "uid://cuo3kxds4641b";
        private const float CursorOffset = 16f;

        [Export] private PanelContainer? _panel;
        [Export] private Label? _title;
        [Export] private RichTextLabel? _description;
        [Export] private Button? _closeButton;

        private Vector2 _pendingPosition;

        public PopupLifetime Lifetime => PopupLifetime.Pinned;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public override void _Ready()
        {
            _closeButton?.Pressed += Close;
            _description?.BbcodeEnabled = true;
            // ShowKeyword runs before the deferred AddChild lands the node in the tree, so the
            // actual placement waits for _Ready (+ one frame for the panel's layout pass)
            Callable.From(Place).CallDeferred();
        }

        /// <summary>Pinned popups close on a click outside the panel.</summary>
        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { Pressed: true }) return;
            if (_panel != null && _panel.GetGlobalRect().HasPoint(_panel.GetGlobalMousePosition())) return;
            Close();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
        }

        public void ShowKeyword(KeywordTooltipView view, Vector2 globalPosition)
        {
            _title?.Text = view.Name;
            _description?.Text = view.Description;
            _pendingPosition = globalPosition;
            Visible = true;
            if (IsInsideTree()) Callable.From(Place).CallDeferred();
        }

        public void Close() => QueueFree();

        private void Place()
        {
            if (_panel != null) UiPlacement.PlaceClamped(_panel, _pendingPosition, new Vector2(CursorOffset, CursorOffset));
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
