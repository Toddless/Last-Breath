namespace Battle.Source.UIElements
{
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Generic hover tooltip: title, an optional dim info line (ability cost/cooldown) and a
    /// BBCode description. Serves ability buttons and stance slots in the battle HUD.
    /// The scene lives in the shared Source tree, so both projects load it by the same path.
    /// </summary>
    [GlobalClass]
    public partial class TextTooltipPopup : Control, IHoverTooltipPopup
    {
        private const string ScenePath = "uid://cpt3b8d6rpa5c";

        [Export] private PanelContainer? _panel;
        [Export] private Label? _title;
        [Export] private Label? _info;
        [Export] private RichTextLabel? _description;

        public PopupLifetime Lifetime => PopupLifetime.WhileHovered;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public bool IsPinned { get; private set; }

        public override void _Ready() => HoverTooltipMotion.Setup(this, _panel);

        public override void _Process(double delta)
        {
            if (!IsPinned) HoverTooltipMotion.Follow(this, _panel);
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            bool pinned = HoverTooltipMotion.TogglePin(@event, IsPinned);
            // Un-pinning dismisses the tooltip: once the pointer has left its source a pinned popup is
            // orphaned (HoverTooltip.Attach dropped its handle on MouseExited), so resuming cursor-follow
            // would make it chase the mouse forever with nothing left to close it.
            if (IsPinned && !pinned) { Close(); return; }
            IsPinned = pinned;
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        public void Show(string title, string? info, string descriptionBbcode)
        {
            _title?.Text = title;
            if (_info != null)
            {
                _info.Text = info ?? string.Empty;
                _info.Visible = !string.IsNullOrEmpty(info);
            }

            _description?.Text = descriptionBbcode;
        }
    }
}
