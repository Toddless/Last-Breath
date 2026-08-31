namespace SharedUi
{
    using Godot;

    /// <summary>
    /// One "caption ... value" line: a dim caption expanding to the left, a highlighted value on the
    /// right. The shared shape behind stat rows, forecast rows and split rows across the windows.
    /// </summary>
    public partial class KeyValueRow : HBoxContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://3eq8b8du6pr5";

        [Export] private Label? _caption;
        [Export] private Label? _value;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>
        /// Fills the row. Without an explicit color the value falls back to the ValueLabel theme
        /// color; passing one (e.g. a rarity color) overrides it.
        /// </summary>
        public void Set(string caption, string value, Color? valueColor = null)
        {
            _caption?.Text = caption;
            if (_value == null) return;
            _value.Text = value;
            if (valueColor is { } color) _value.AddThemeColorOverride("font_color", color);
            else _value.RemoveThemeColorOverride("font_color");
        }
    }
}
