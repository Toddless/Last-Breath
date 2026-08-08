namespace Battle.Source.UIElements.PassiveWheel
{
    using Godot;

    /// <summary>
    /// One wedge of the backdrop: where it points, what colour it tints and what it is called. Pure
    /// decoration for the eye and a landmark for the hand — nothing snaps to it and no node reads it.
    /// </summary>
    [GlobalClass]
    public partial class WheelSector : Resource
    {
        /// <summary>Screen space, degrees, 0 = right and growing clockwise — the direction the wedge
        /// is centred on.</summary>
        [Export] public float AngleDegrees { get; set; }

        [Export] public Color Tint { get; set; } = Colors.White;

        /// <summary>Localisation key rather than the word itself: the wheel is a screen the player
        /// reads, and its wedges are named in his language.</summary>
        [Export] public string LabelKey { get; set; } = string.Empty;
    }
}
