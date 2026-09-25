namespace SharedUi
{
    using Godot;

    /// <summary>
    /// The bare Umbral tooltip frame: a mouse-transparent root hosting a TooltipPanel-styled panel
    /// with an empty content VBox. Phase 2 tooltips build their rows inside <see cref="Content"/>;
    /// the frame itself owns nothing but the chrome.
    /// </summary>
    public partial class TooltipFrame : Control
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://bkfc46l6wnkxy";

        [Export] private VBoxContainer? _content;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>The VBox the tooltip's rows are added into.</summary>
        public Node? Content => _content;
    }
}
