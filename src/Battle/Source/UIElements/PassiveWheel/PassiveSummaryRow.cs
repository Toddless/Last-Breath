namespace Battle.Source.UIElements.PassiveWheel
{
    using Godot;

    /// <summary>
    /// One line of the totals panel: what it is, and the buckets it lands in. A column with nothing in it
    /// is hidden rather than filled with a zero — a table of zeroes is harder to read than a short one.
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

        public void Show(string name, string flat, string increase, string more, string total, string delta)
        {
            _name?.Text = name;
            Fill(_flat, flat);
            Fill(_increase, increase);
            Fill(_more, more);
            Fill(_total, total);
            Fill(_delta, delta);
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        private static void Fill(Label? label, string text)
        {
            if (label == null) return;

            label.Text = text;
            label.Visible = text.Length > 0;
        }
    }
}
