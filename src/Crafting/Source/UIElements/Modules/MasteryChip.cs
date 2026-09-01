namespace Crafting.Source.UIElements.Modules
{
    using Godot;

    /// <summary>
    /// One bonus-channel chip of the mastery rail: the dim channel caption, the bonus value and the
    /// mini progress bar underneath. Fonts, margins and the bar shape live in
    /// <c>MasteryChip.tscn</c> — the rail only instantiates and fills.
    /// </summary>
    public partial class MasteryChip : PanelContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://dmchip6qv3r8k";

        [Export] private Label? _title;
        [Export] private Label? _value;
        [Export] private ProgressBar? _bar;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Fills the chip: channel caption, the formatted bonus and the shared level factor.</summary>
        public void Set(string title, string value, float progress)
        {
            _title?.Text = title;
            _value?.Text = value;
            _bar?.Value = progress;
        }
    }
}
