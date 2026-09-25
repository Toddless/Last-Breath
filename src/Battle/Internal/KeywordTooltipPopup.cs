namespace Battle.Internal
{
    using Core.Localization;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    [GlobalClass]
    internal partial class KeywordTooltipPopup : Control, IKeywordTooltipPopup
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

        public void ShowKeyword(KeywordTooltipView view, Vector2 globalPosition)
        {
            _title?.Text = view.Name;
            TintTitle(view.TitleColorHex);
            _description?.Text = view.Description;
            _pendingPosition = globalPosition;
            Visible = true;
            if (IsInsideTree()) Callable.From(Place).CallDeferred();
        }

        public void Close() => QueueFree();

        /// <summary>Damage-type cards title themselves in their type's color; a null hex leaves the
        /// themed title alone (Effect_* cards look as they always did). A LabelSettings resource is
        /// duplicated before tinting — it is shared by every instance of the scene.</summary>
        private void TintTitle(string? colorHex)
        {
            if (_title == null || colorHex == null) return;
            var color = new Color(colorHex);
            if (_title.LabelSettings is { } settings)
            {
                var tinted = (LabelSettings)settings.Duplicate();
                tinted.FontColor = color;
                _title.LabelSettings = tinted;
            }
            else _title.AddThemeColorOverride("font_color", color);
        }

        private void Place()
        {
            if (_panel != null) UiPlacement.PlaceClamped(_panel, _pendingPosition, new Vector2(CursorOffset, CursorOffset));
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
