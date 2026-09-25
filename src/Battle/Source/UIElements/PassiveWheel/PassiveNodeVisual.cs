namespace Battle.Source.UIElements.PassiveWheel
{
    using Core.PassiveTree;
    using Godot;

    /// <summary>
    /// How one class of node looks and how it is drawn. One row per class in
    /// <see cref="PassiveWheelStyle"/>, so a new class is an entry in a resource rather than a sweep
    /// through the drawing code.
    /// </summary>
    [GlobalClass]
    public partial class PassiveNodeVisual : Resource
    {
        [Export] public PassiveNodeKind Kind { get; set; }

        /// <summary>The authored size in document units, at zoom 1. A size, so it follows the zoom
        /// alone — the layout spread moves nodes apart without making any of them bigger.</summary>
        [Export] public float Radius { get; set; } = 6f;

        /// <summary>How far the glow reaches, in document units — always wider than the body, which is
        /// the whole point of it. Authored beside the radius because the two textures are normalised to
        /// different sizes and a glow scaled by the body's number would be cropped to the body.</summary>
        [Export] public float GlowRadius { get; set; } = 12f;

        /// <summary>
        /// The body texture. Authored at <c>2 × Radius × PassiveWheelStyle.TextureOversample</c> pixels
        /// and NOT at twice the radius: the wheel zooms in past 1, so a texture the size of the node at
        /// zoom 1 is stretched by the card at every zoom above it and the player reads a blurred dot. The
        /// sprite is normalised by the SIZE OF THE TEXTURE rather than by the class, so an artist who
        /// brings a sharper one changes nothing but the file.
        /// </summary>
        [Export] public Texture2D? Body { get; set; }

        [Export] public Texture2D? BodyTaken { get; set; }

        /// <summary>Authored at <c>2 × GlowRadius × PassiveWheelStyle.TextureOversample</c> pixels, by
        /// the rule <see cref="Body"/> is authored under.</summary>
        [Export] public Texture2D? Glow { get; set; }

        /// <summary>
        /// Whether the class is drawn as a scene of its own instead of as part of the mass. This is the
        /// cut between the two node layers, and it is a flag rather than a test on the class: if a class
        /// grows from dozens into hundreds it moves into the mass by editing this resource, and if the
        /// mass ever needs to animate it moves out the same way.
        /// </summary>
        [Export] public bool HasView { get; set; }
    }
}
