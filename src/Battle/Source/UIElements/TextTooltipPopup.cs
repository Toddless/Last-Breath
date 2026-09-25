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

        public override void _UnhandledKeyInput(InputEvent @event) =>
            IsPinned = HoverTooltipMotion.HandlePinInput(this, @event, IsPinned);

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        /// <param name="titleColor">What the title is painted in, for the cards whose title carries a
        /// meaning of its own — an augment's rarity. Left out, the title is not touched at all and keeps
        /// exactly the look the scene gave it, which is what every caller that has nothing to say with
        /// the colour wants.</param>
        public void Show(string title, string? info, string descriptionBbcode, Color? titleColor = null)
        {
            if (_title != null)
            {
                _title.Text = title;
                Paint(_title, titleColor);
            }

            if (_info != null)
            {
                _info.Text = info ?? string.Empty;
                _info.Visible = !string.IsNullOrEmpty(info);
            }

            _description?.Text = descriptionBbcode;
        }

        /// <summary>
        /// Puts the colour where the label actually reads one from. A label carrying LabelSettings takes
        /// its colour from that resource and ignores the theme entirely, so a theme override on this one
        /// would be written and never drawn.
        /// <para>The settings in the scene are ONE object shared by every instance of it: painting it
        /// would repaint every tooltip in the process, so the popup copies it first and wears the copy.
        /// A caller naming no colour is left alone — no copy, no allocation, and a title that looks
        /// exactly as it shipped. There is no colour to clear on the way in either: a popup is a fresh
        /// instance shown once, so what it starts with IS what the scene shipped.</para>
        /// </summary>
        private static void Paint(Label title, Color? color)
        {
            if (color == null) return;

            if (title.LabelSettings is not { } shared)
            {
                // No settings resource: the theme is what the label reads, and an override reaches it.
                title.AddThemeColorOverride("font_color", color.Value);
                return;
            }

            var own = (LabelSettings)shared.Duplicate();
            own.FontColor = color.Value;
            title.LabelSettings = own;
        }
    }
}
