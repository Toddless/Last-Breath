namespace SharedUi
{
    using Godot;

    /// <summary>
    /// One tier/stage line: a small framed number plus the stage description. The text is a
    /// RichTextLabel (bbcode, autowrap); the row's ExpandFill keeps its width stable so autowrap
    /// cannot inflate the height (the known Godot trap).
    /// </summary>
    public partial class TierRow : HBoxContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://bwqrh1s43klrj";

        [Export] private Label? _number;
        [Export] private RichTextLabel? _text;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Set(int number, string bbcode)
        {
            _number?.Text = number.ToString();
            _text?.Text = bbcode;
        }
    }
}
