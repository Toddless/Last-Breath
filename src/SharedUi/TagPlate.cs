namespace SharedUi
{
    using Godot;

    /// <summary>
    /// A small framed plate with one tag word (ability/item tags). Styled by the TagPlate theme
    /// variation; usually spawned in bulk by <see cref="TagRow"/>.
    /// </summary>
    public partial class TagPlate : PanelContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://dh4wepgj3g1sw";

        [Export] private Label? _label;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetTag(string text) => _label?.Text = text;
    }
}
