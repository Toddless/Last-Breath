namespace Crafting.Source.CraftingResources
{
    using Core.Enums;
    using Core.Interfaces.CraftingResources;
    using Godot;

    [GlobalClass]
    public partial class MaterialModifiers : Resource, IMaterialModifiers
    {
        [Export] public Parameter Parameter { get; set; }
        [Export] public ModifierType ModifierType { get; set; }
        [Export] public float Value { get; set; }
    }
}
