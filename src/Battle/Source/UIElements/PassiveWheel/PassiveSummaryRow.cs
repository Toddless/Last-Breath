namespace Battle.Source.UIElements.PassiveWheel
{
    using Godot;

    /// <summary>
    /// One line of the totals panel: what it is, and the buckets it lands in. An empty column keeps its
    /// cell — a hidden child leaves the row, and the row that lost a cell slides its numbers under the
    /// wrong heading.
    /// <para>It formats nothing. Every number arrives as the string the game's own formatter produced, so
    /// a passive total reads in exactly the units an item tooltip reads the same parameter in.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveSummaryRow : HBoxContainer
    {
        /// <summary>The uid the scene file declares in its own header — the authority, so a copy that
        /// drifts from it resolves to nothing at all in the game project.</summary>
        private const string UID = "uid://dsr6knc4mfp8t";

        [Export] private Label? _name;
        [Export] private Label? _flat;
        [Export] private Label? _increase;
        [Export] private Label? _more;
        [Export] private Label? _total;
        [Export] private Label? _delta;

        /// <summary>The scene still carries a cell for the folded-together total, and nothing fills it:
        /// with no base value and no gear behind it, that column could only ever restate the flat bucket.
        /// Hidden here, once and for every row alike, so the headings and the numbers keep counting the
        /// same columns.</summary>
        public override void _Ready()
        {
            if (_total != null) _total.Visible = false;
        }

        public void Show(string name, string flat, string increase, string more, string delta)
        {
            _name?.Text = name;
            _flat?.Text = flat;
            _increase?.Text = increase;
            _more?.Text = more;
            _delta?.Text = delta;
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);
    }
}
