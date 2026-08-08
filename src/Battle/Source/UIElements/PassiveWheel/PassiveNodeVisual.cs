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

        [Export] public Texture2D? Body { get; set; }

        [Export] public Texture2D? BodyTaken { get; set; }

        [Export] public Texture2D? Glow { get; set; }

        /// <summary>
        /// Whether the class is drawn as a scene of its own instead of as part of the mass. This is the
        /// cut between the two node layers, and it is a flag rather than a test on the class: if a class
        /// grows from dozens into hundreds it moves into the mass by editing this resource, and if the
        /// mass ever needs to animate it moves out the same way.
        /// </summary>
        [Export] public bool HasView { get; set; }

        /// <summary>Whether the node wears its name on the wheel above the label zoom threshold.
        /// Captions on every small node turn the wheel into noise.</summary>
        [Export] public bool Labelled { get; set; }
    }
}
