namespace Battle.Source.UIElements.PassiveWheel
{
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>
    /// The catalog of authored node looks (the AbilityVisualLibrary pattern): rows addressed by node id or
    /// by node class, in one resource assigned to the wheel in the editor. A wheel with no library — or
    /// with a library saying nothing about a node — draws what it always drew, so the channel is pure
    /// opt-in data and a half-filled resource is a legal one.
    /// </summary>
    [GlobalClass]
    public partial class PassiveNodeVisualLibrary : Resource
    {
        [Export] public Godot.Collections.Array<PassiveNodeVisualConfig> Configs { get; set; } = [];

        /// <summary>The lookup the wheel reads through. Built once by whoever holds the library rather
        /// than per node: the drawing asks about every node of the document on every redraw.</summary>
        public PassiveNodeVisualIndex<PassiveNodeVisualConfig> Index() => new(Configs);
    }
}
