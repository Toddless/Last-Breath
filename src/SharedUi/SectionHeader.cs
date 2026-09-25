namespace SharedUi
{
    using Godot;

    /// <summary>
    /// Umbral section header: an uppercase gold title with a golden divider line fading toward the
    /// right edge. One shared scene instead of the ad-hoc header rows every window used to build.
    /// </summary>
    public partial class SectionHeader : HBoxContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://buwn6bbogk42v";

        [Export] private Label? _title;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>
        /// Sets the section title. The theme cannot capitalize, so the text itself is uppercased here.
        /// </summary>
        public void SetTitle(string title) => _title?.Text = title.ToUpperInvariant();
    }
}
