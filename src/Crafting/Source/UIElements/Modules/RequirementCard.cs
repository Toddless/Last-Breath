namespace Crafting.Source.UIElements.Modules
{
    using Godot;
    using SharedUi;

    /// <summary>
    /// The content of one requirement/additive card: the icon+name row with the have/need counter,
    /// margins and fonts authored in <c>RequirementCard.tscn</c>. The shells stay the panel's —
    /// a static PanelContainer or the clickable slot Button — and this card overlays them
    /// mouse-transparently.
    /// </summary>
    public partial class RequirementCard : MarginContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://creqcard5tv2n";

        [Export] private IconLabelRow? _row;
        [Export] private Label? _count;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>
        /// Fills the card; a null icon hides the icon slot, a null count hides the have/need column
        /// (additive cards). The lone "+" of an empty additive slot (no icon, no count) centers; an
        /// unmet count dims.
        /// </summary>
        public void Set(Texture2D? icon, string name, string? count, bool countMet)
        {
            // The empty additive slot is a lone "+" — center it; real rows read left to right.
            _row?.Alignment = icon == null && count == null ? BoxContainer.AlignmentMode.Center : BoxContainer.AlignmentMode.Begin;
            _row?.Set(icon, name);
            _row?.MakeMouseTransparent();
            if (_count == null) return;
            _count.Visible = count != null;
            _count.Text = count ?? string.Empty;
            _count.ThemeTypeVariation = countMet ? string.Empty : "DimLabel";
        }
    }
}
