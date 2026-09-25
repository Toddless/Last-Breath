namespace Battle.Source.UIElements.PassiveWheel
{
    using Godot;

    /// <summary>
    /// One wedge of the backdrop: where it points and what colour it tints. Pure decoration for the eye
    /// and a landmark for the hand — nothing snaps to it, no node reads it, and it wears no caption: the
    /// wheel is read by the shape of its rays and not by six words floating over them.
    /// </summary>
    [GlobalClass]
    public partial class WheelSector : Resource
    {
        /// <summary>Screen space, degrees, 0 = right and growing clockwise — the direction the wedge
        /// is centred on.</summary>
        [Export] public float AngleDegrees { get; set; }

        [Export] public Color Tint { get; set; } = Colors.White;
    }
}
