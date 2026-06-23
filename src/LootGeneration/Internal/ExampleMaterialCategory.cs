namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Crafting;
    using Core.Modifiers;
    using Godot;

    internal partial class ExampleMaterialCategory: Resource, IMaterialCategory
    {
    [Export] private ExampleMaterialModifier[] _modifiers = [];
    [Export] public string Id { get; set; } = string.Empty;
    public IReadOnlyList<IModifier> Modifiers => _modifiers;

    // we need to create a default, parameterless constructor in order to create a resource from within the Godot Editor.
    public ExampleMaterialCategory()
    {

    }
    /// <summary>
    /// Constructor to create a resource within code
    /// </summary>
    /// <param name="modifiers"></param>
    /// <param name="id"></param>
    public ExampleMaterialCategory(List<IModifier> modifiers, string id)
    {
        _modifiers = [.. modifiers.Cast<ExampleMaterialModifier>()];
        Id = id;
    }
    }
}
