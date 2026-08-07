namespace Battle.Source.UIElements
{
    using Godot;

    /// <summary>
    /// One row of the ability's augment list: the augment's name and what this copy of it does. It is
    /// built on a Button because the layout it sits in is, and it is never a control — an ability wears
    /// exactly what its sockets hold, so there is nothing here to press. Seating and extracting happen
    /// through the socket window and its request gates.
    /// </summary>
    [GlobalClass]
    public partial class UpgradeButton : Button
    {
        [Export] private RichTextLabel? _description;

        /// <summary>Fills the row and takes the press away: pressed marks it as worn, disabled makes
        /// sure the mark cannot be moved by a click that would go nowhere.</summary>
        public void ShowWorn(string displayName, string description)
        {
            Text = displayName;
            _description?.Text = description;
            SetPressedNoSignal(true);
            Disabled = true;
        }
    }
}
