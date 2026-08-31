namespace SharedUi
{
    using Godot;

    /// <summary>
    /// One "icon · label · trailing" line: an optional icon on the left, the name expanding in the
    /// middle, and whatever the consumer appends on the right (have/need counters, prices, buy
    /// buttons). The shared shape behind the crafting requirement cards and the trade offer rows.
    /// </summary>
    public partial class IconLabelRow : HBoxContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://cfg3sk27iolr4";

        [Export] private TextureRect? _icon;
        [Export] private Label? _label;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Icon edge in pixels (the crafting cards keep the 28 default, trade rows use 32).</summary>
        public float IconSize
        {
            get => _icon?.CustomMinimumSize.X ?? 0f;
            set => _icon?.CustomMinimumSize = new Vector2(value, value);
        }

        /// <summary>
        /// Fills the row; a null icon hides the slot entirely (category "choose…" cards, the lone
        /// "+" of an empty additive slot). Without an explicit color the label keeps the theme
        /// tone; passing one (e.g. a rarity color) overrides it.
        /// </summary>
        public void Set(Texture2D? icon, string label, Color? labelColor = null)
        {
            _icon?.Visible = icon != null;
            _icon?.Texture = icon;
            if (_label == null) return;
            _label.Text = label;
            if (labelColor is { } color) _label.AddThemeColorOverride("font_color", color);
            else _label.RemoveThemeColorOverride("font_color");
        }

        /// <summary>Appends consumer-owned controls after the label (counter, price, button).</summary>
        public void SetTrailing(params Control[] trailing)
        {
            foreach (var control in trailing) AddChild(control);
        }

        /// <summary>Single-line mode: a long name trims with an ellipsis instead of wrapping
        /// (trade rows, where buttons keep the line height fixed).</summary>
        public void UseEllipsis()
        {
            if (_label == null) return;
            _label.AutowrapMode = TextServer.AutowrapMode.Off;
            _label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        }

        /// <summary>Content overlaying a clickable card (the crafting slot buttons) must not eat
        /// the card's mouse events. Call after the trailing controls are in.</summary>
        public void MakeMouseTransparent()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            foreach (var child in GetChildren())
                if (child is Control control) control.MouseFilter = MouseFilterEnum.Ignore;
        }
    }
}
