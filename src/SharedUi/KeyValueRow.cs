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
        private RichTextLabel? _richValue;

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

        /// <summary>
        /// Same row, but the value side is bbcode (the forecast's struck-through "was → now").
        /// The plain value Label hides behind a lazily created RichTextLabel; a min width keeps
        /// the value column stable across rows.
        /// </summary>
        public void SetRich(string caption, string valueBbcode, float valueMinWidth = 0f)
        {
            _caption?.Text = caption;
            _value?.Visible = false;
            _richValue ??= CreateRichValue();
            _richValue.Text = valueBbcode;
            _richValue.CustomMinimumSize = new Vector2(valueMinWidth, 0);
        }

        /// <summary>Regular label tone for the caption instead of the dim one; a tint (an affix
        /// family colour) overrides further.</summary>
        public void SetPlainCaption(Color? tint = null)
        {
            if (_caption == null) return;
            _caption.ThemeTypeVariation = "";
            if (tint is { } color) _caption.AddThemeColorOverride("font_color", color);
            else _caption.RemoveThemeColorOverride("font_color");
        }

        /// <summary>Regular label tone for the value instead of the ValueLabel accent (the
        /// character sheet's stat numbers).</summary>
        public void UsePlainValue() => _value?.ThemeTypeVariation = "";

        /// <summary>Long captions (modifier pool lines) wrap instead of stretching the window;
        /// the value then centers vertically beside the taller caption.</summary>
        public void EnableCaptionAutowrap()
        {
            _caption?.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _value?.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        }

        private RichTextLabel CreateRichValue()
        {
            var rich = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                AutowrapMode = TextServer.AutowrapMode.Off,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            AddChild(rich);
            return rich;
        }
    }
}
